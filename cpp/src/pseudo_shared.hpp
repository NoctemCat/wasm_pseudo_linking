#include <cstddef>
#include <cstdint>
#include <cstring>
#include <emscripten/em_macros.h>
#include <limits>

EMSCRIPTEN_KEEPALIVE int main();

template <std::size_t s>
constexpr int32_t safe_int_sizeof() noexcept {
    static_assert(s <= std::numeric_limits<int32_t>::max(), "Type too large for ssizeof()!");
    return static_cast<int32_t>(s);
}

#define ssizeof(x) safe_int_sizeof<sizeof(x)>()

typedef void *(*rb_bridge_func)(const uintptr_t lib_id, const char *name);

class MainMemory {
public:
    using ptr_type = void *;

private:
    typedef void (*set_i32_t)(ptr_type, const int32_t);
    typedef int32_t (*get_i32_t)(const ptr_type);
    typedef void (*mts_memcpy_f)(void *dest, const ptr_type src, const int32_t);
    typedef void (*stm_memcpy_f)(ptr_type dest, const void *src, const int32_t);

    static inline set_i32_t set_i32_f = nullptr;
    static inline get_i32_t get_i32_f = nullptr;

public:
    static inline mts_memcpy_f mts_memcpy = nullptr;
    static inline stm_memcpy_f stm_memcpy = nullptr;
    static inline mts_memcpy_f mts_memcpy_fb = nullptr;
    static inline stm_memcpy_f stm_memcpy_fb = nullptr;

    static void set_i32(ptr_type addr, const int32_t value) { set_i32_f(addr, value); }

    static void set_i32(ptr_type addr, const int32_t offset, const int32_t value) {
        set_i32_f(static_cast<uint8_t *>(addr) + (ssizeof(int32_t) * offset), value);
    }

    static int32_t get_i32(const ptr_type addr) { return get_i32_f(addr); }

    static int32_t get_i32(const ptr_type addr, const int32_t offset) {
        return get_i32_f(static_cast<uint8_t *>(addr) + (ssizeof(int32_t) * offset));
    }

    static void init(const rb_bridge_func rb_bridge, const uintptr_t self_id) {
        set_i32_f = reinterpret_cast<set_i32_t>(rb_bridge(0, "setI32"));
        get_i32_f = reinterpret_cast<get_i32_t>(rb_bridge(0, "getI32"));
        mts_memcpy = reinterpret_cast<mts_memcpy_f>(rb_bridge(self_id, "mts_memcpy"));
        stm_memcpy = reinterpret_cast<stm_memcpy_f>(rb_bridge(self_id, "stm_memcpy"));

        rb_bridge_func rb_bridge_fb =
            reinterpret_cast<rb_bridge_func>(rb_bridge(self_id, "rbBridgeFb"));
        mts_memcpy_fb = reinterpret_cast<mts_memcpy_f>(rb_bridge_fb(self_id, "mts_memcpy"));
        stm_memcpy_fb = reinterpret_cast<stm_memcpy_f>(rb_bridge_fb(self_id, "stm_memcpy"));
    }
};

extern "C" int32_t main_getI32(const void *mainPtr);
extern "C" void main_setI32(void *mainPtr, int32_t value);

extern "C" void mts_memcpy(void *dest, const void *src, int32_t size);
extern "C" void stm_memcpy(void *dest, const void *src, int32_t size);

extern "C" void mts_memcpy_fb(void *dest, const void *src, int32_t size);
extern "C" void stm_memcpy_fb(void *dest, const void *src, int32_t size);

extern "C" void *mts_addFnPtr(void *fn_ptr);
extern "C" void *stm_addFnPtr(void *fn_ptr);

class MainMemoryImport {
public:
    using ptr_type = void *;

    static int32_t get_i32(const ptr_type addr) { return ::main_getI32(addr); }

    static int32_t get_i32(const ptr_type addr, const int32_t offset) {
        return ::main_getI32(static_cast<uint8_t *>(addr) + (ssizeof(int32_t) * offset));
    }

    static void set_i32(ptr_type addr, const int32_t value) { ::main_setI32(addr, value); }

    static void set_i32(ptr_type addr, const int32_t offset, const int32_t value) {
        ::main_setI32(static_cast<uint8_t *>(addr) + (ssizeof(int32_t) * offset), value);
    }

    static void mts_memcpy(void *dest, const ptr_type src, const int32_t size) {
        ::mts_memcpy(dest, src, size);
    }

    static void stm_memcpy(ptr_type dest, const void *src, const int32_t size) {
        ::stm_memcpy(dest, src, size);
    }

    static void mts_memcpy_fb(void *dest, const ptr_type src, const int32_t size) {
        ::mts_memcpy_fb(dest, src, size);
    }

    static void stm_memcpy_fb(ptr_type dest, const void *src, const int32_t size) {
        ::stm_memcpy_fb(dest, src, size);
    }

    static void *mts_add_fn_ptr(void *fn_ptr) { return ::mts_addFnPtr(fn_ptr); }

    static void *stm_add_fn_ptr(void *fn_ptr) { return ::stm_addFnPtr(fn_ptr); }
};

struct MainPtr {
    MainMemory::ptr_type ptr;

    int32_t get_i32() const { return MainMemory::get_i32(ptr); }

    void set_i32(int32_t value) { MainMemory::set_i32(ptr, value); }
};

extern "C" {
EMSCRIPTEN_KEEPALIVE void sub(
    const MainMemory::ptr_type a, const MainMemory::ptr_type b, MainMemory::ptr_type ret
);
EMSCRIPTEN_KEEPALIVE void sort_array(MainMemory::ptr_type begin_ptr, int32_t length);
EMSCRIPTEN_KEEPALIVE void sort_array_fallback(MainMemory::ptr_type begin_ptr, int32_t length);
EMSCRIPTEN_KEEPALIVE void sort_array_import(MainMemoryImport::ptr_type begin_ptr, int32_t length);
EMSCRIPTEN_KEEPALIVE void sort_array_import_fallback(
    MainMemoryImport::ptr_type begin_ptr, int32_t length
);
EMSCRIPTEN_KEEPALIVE void sort_array_direct(MainMemory::ptr_type begin_ptr, int32_t length);
EMSCRIPTEN_KEEPALIVE void sort_array_direct_import(
    MainMemoryImport::ptr_type begin_ptr, int32_t length
);

EMSCRIPTEN_KEEPALIVE void init_marshalling(const rb_bridge_func func, uintptr_t self_id);
}