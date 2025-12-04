#include "emscripten/em_macros.h"
#include "main_sort.hpp"
#include <cstddef>
#include <cstdint>
#include <dlfcn.h>
#include <iostream>
#include <ostream>
#include <utility>

typedef void (*arithmetic_func)(const int32_t *, const int32_t *, int32_t *);
typedef void (*arithmetic_func_side)(const void *, const void *, void *);

extern "C" {
void *rb_dlopen(const char *name);
void *rb_dlsym(void *lib_ptr, const char *fn_name);
void *rb_bridge(void *lib_ptr, const char *fn_name);

bool rb_log_enabled();

EMSCRIPTEN_KEEPALIVE int add(int a, int b) { return a + b; }
}

template <typename T>
T rb_dlsym(void *lib_ptr, const char *fn_name) {
    return reinterpret_cast<T>(rb_dlsym(lib_ptr, fn_name));
}

template <typename T>
T rb_bridge(void *lib_ptr, const char *fn_name) {
    return reinterpret_cast<T>(rb_bridge(lib_ptr, fn_name));
}

class RbLog {
public:
    template <class T>
    RbLog &operator<<(T &&t) {
        if (rb_log_enabled()) {
            std::cout << std::forward<T>(t);
        }
        return *this;
    }

    RbLog &operator<<(std::ostream &(*f)(std::ostream &o)) {
        if (rb_log_enabled()) {
            std::cout << f;
        }
        return *this;
    };
};

RbLog rb_log{};

void load_direct() {
    void *lib_handle = dlopen("./shared_direct.wasm", RTLD_LAZY);
    rb_log << "direct dlopen " << lib_handle << "\n";
    arithmetic_func func = reinterpret_cast<arithmetic_func>(dlsym(lib_handle, "sub"));
    rb_log << "direct dlsym " << (void *)func << "\n";
    int32_t a = 10, b = 100, ret;
    func(&a, &b, &ret);
    std::cout << "call direct: " << ret << std::endl;

    sort_array = reinterpret_cast<sort_array_fn>(dlsym(lib_handle, "sort_array"));
}

void load_rb() {
    typedef void *(*mts_add_fn_ptr_fn)(const void *rb_bridge_func);
    typedef void (*init_fn)(const void *rb_bridge_side_frptr, const void *self_id);

    void *pseudo_handle = rb_dlopen("pseudo_shared");
    rb_log << "pseudo rb_dlopen " << pseudo_handle << "\n";

    mts_add_fn_ptr_fn pseudo_add_fn = rb_bridge<mts_add_fn_ptr_fn>(pseudo_handle, "mts_addFnPtr");
    void *side_rb_bridge = rb_bridge(pseudo_handle, "rbBridge");
    rb_log << "pseudo rb_bridge" << "\n";

    init_fn init_marshalling = rb_dlsym<init_fn>(pseudo_handle, "init_marshalling");
    arithmetic_func_side pseudo_sub = rb_dlsym<arithmetic_func_side>(pseudo_handle, "sub");
    rb_log << "pseudo rb_dlsym" << "\n";

    init_marshalling(pseudo_add_fn(side_rb_bridge), pseudo_handle);

    int32_t a = 400, b = 123, ret;
    pseudo_sub(&a, &b, &ret);
    std::cout << "call rb " << ret << std::endl;

#define GET_SORT(sort_type)                                                                        \
    sort_array_rb_##sort_type = rb_dlsym<sort_array_rbfn>(pseudo_handle, "sort_array_" #sort_type)

    sort_array_rb = rb_dlsym<sort_array_rbfn>(pseudo_handle, "sort_array");
    GET_SORT(fallback);
    GET_SORT(import);
    GET_SORT(import_fallback);
    GET_SORT(direct);
    GET_SORT(direct_import);
#undef GET_SORT
}

void load_net() {
    typedef void *(*mts_add_fn_ptr_fn)(void *rb_bridge_func);
    typedef void *(*net_malloc_fn)(int32_t);
    typedef void (*net_free_fn)(void *);
    typedef void (*net_memcpy_fn)(void *dest, const void *src, int32_t);
    typedef void *(*net_entry_fn)(const void *rb_bridge_side_frptr, const void *self_id);
    typedef void *(*net_dlsym_fn)(const void *);

    void *net_handle = rb_dlopen("pseudo_net");

    mts_add_fn_ptr_fn net_add = rb_bridge<mts_add_fn_ptr_fn>(net_handle, "mts_addFnPtr");
    net_memcpy_fn net_memcpy = rb_bridge<net_memcpy_fn>(net_handle, "mts_memcpy");
    void *net_rb_bridge = rb_bridge(net_handle, "rbBridge");

    net_malloc_fn net_malloc = rb_dlsym<net_malloc_fn>(net_handle, "malloc");
    net_free_fn net_free = rb_dlsym<net_free_fn>(net_handle, "free");
    net_entry_fn net_entry = rb_dlsym<net_entry_fn>(net_handle, "EntryPoint");
    rb_log << "after net get EntryPoint \n";

    const net_dlsym_fn net_dlsym_side =
        reinterpret_cast<net_dlsym_fn>(net_entry(net_add(net_rb_bridge), net_handle));
    rb_log << "net EntryPoint called \n";

    auto net_dlsym = [net_malloc, net_memcpy, net_dlsym_side, net_free](std::string fn_name) {
        void *net_sub_ptr = net_malloc(fn_name.size() + 1);
        net_memcpy(net_sub_ptr, (void *)fn_name.c_str(), fn_name.size() + 1);
        void *net_fn = net_dlsym_side(net_sub_ptr);
        net_free(net_sub_ptr);
        return net_fn;
    };

    arithmetic_func_side net_sub = reinterpret_cast<arithmetic_func_side>(net_dlsym("Sub"));

    rb_log << "before net sub \n";
    int32_t a = 500, b = 1, ret;
    net_sub(&a, &b, &ret);
    std::cout << "call net " << ret << std::endl;

#define GET_SORT(sort_type, name)                                                                  \
    sort_array_net_##sort_type = reinterpret_cast<sort_array_rbfn>(net_dlsym("SortArray" name))

    sort_array_net = reinterpret_cast<sort_array_rbfn>(net_dlsym("SortArray"));
    GET_SORT(fallback, "Fallback");
    GET_SORT(import, "Import");
    GET_SORT(import_fallback, "ImportFallback");
    GET_SORT(direct, "Direct");
    GET_SORT(direct_import, "DirectImport");
#undef GET_SORT
}

EMSCRIPTEN_KEEPALIVE int main() {
    rb_log << "main started\n";
    load_direct();
    rb_log << "after load_direct\n";
    load_rb();
    rb_log << "after load_rb\n";
    load_net();
    rb_log << "after load_net\n";
    validate_sort_equality(10, 100, false);
}
