#pragma once
#include <cstdint>
#include <emscripten/em_macros.h>
#include <ostream>

template <typename el_type>
std::ostream &operator<<(std::ostream &os, const std::vector<el_type> &arr) {
    if (arr.empty())
        return os;
    os << "[" << arr[0];
    if (arr.size() > 1) {
        for (size_t i = 1; i < arr.size(); i++) {
            os << ", " << arr[i];
        }
    }
    os << "]";

    return os;
}

typedef void (*sort_array_fn)(int32_t *, int32_t);
typedef void (*sort_array_rbfn)(void *, int32_t);

int32_t get_random();
extern "C" EMSCRIPTEN_KEEPALIVE bool validate_sort_equality(
    int32_t length, int32_t variation, bool silent
);

extern "C" EMSCRIPTEN_KEEPALIVE int32_t *allocate_random_array(int32_t arr_size);
extern "C" EMSCRIPTEN_KEEPALIVE int32_t *allocate_array(int32_t arr_size);
extern "C" EMSCRIPTEN_KEEPALIVE void free_array(int32_t *arr);

extern sort_array_fn sort_array;
extern "C" EMSCRIPTEN_KEEPALIVE double sort(
    const int32_t *src_arr, int32_t *dest_arr, int32_t arr_size
);

#define EXPORT_SORT(sort_type)                                                                     \
    extern sort_array_rbfn sort_array_##sort_type;                                                 \
    EMSCRIPTEN_KEEPALIVE extern "C" double sort_##sort_type(                                       \
        const int32_t *src_arr, int32_t *dest_arr, int32_t arr_size                                \
    )

EXPORT_SORT(rb);
EXPORT_SORT(rb_import);
EXPORT_SORT(rb_fallback);
EXPORT_SORT(rb_import_fallback);
EXPORT_SORT(rb_direct);
EXPORT_SORT(rb_direct_import);
EXPORT_SORT(net);
EXPORT_SORT(net_import);
EXPORT_SORT(net_fallback);
EXPORT_SORT(net_import_fallback);
EXPORT_SORT(net_direct);
EXPORT_SORT(net_direct_import);

#undef EXPORT_SORT