
#include "pseudo_shared.hpp"
#include <cstdint>

int main() {}

void sub(const MainMemory::ptr_type a, const MainMemory::ptr_type b, MainMemory::ptr_type ret) {
    MainPtr aPtr{a}, bPtr{b}, retPtr{ret};
    retPtr.set_i32(aPtr.get_i32() - bPtr.get_i32());
}

void quicksort(int32_t *array, const int32_t start, const int32_t end) {
    if (start >= end)
        return;
    const int32_t pivot = array[end];
    int32_t left = 0;
    int32_t right = 0;
    while (left + right < end - start) {
        const int32_t num = array[start + left];
        if (num < pivot) {
            left++;
        } else {
            array[start + left] = array[end - right - 1];
            array[end - right - 1] = pivot;
            array[end - right] = num;
            right++;
        }
    }
    quicksort(array, start, start + left - 1);
    quicksort(array, start + left + 1, end);
}

#define IMPL_DIRECT_SORT(type, memory_type)                                                        \
    void quicksort_##type(memory_type::ptr_type array, const int32_t start, const int32_t end) {   \
        if (start >= end)                                                                          \
            return;                                                                                \
        const int32_t pivot = memory_type::get_i32(array, end);                                    \
        int32_t left = 0;                                                                          \
        int32_t right = 0;                                                                         \
        while (left + right < end - start) {                                                       \
            const int32_t num = memory_type::get_i32(array, start + left);                         \
            if (num < pivot) {                                                                     \
                left++;                                                                            \
            } else {                                                                               \
                memory_type::set_i32(                                                              \
                    array, start + left, memory_type::get_i32(array, end - right - 1)              \
                );                                                                                 \
                memory_type::set_i32(array, end - right - 1, pivot);                               \
                memory_type::set_i32(array, end - right, num);                                     \
                right++;                                                                           \
            }                                                                                      \
        }                                                                                          \
        quicksort_##type(array, start, start + left - 1);                                          \
        quicksort_##type(array, start + left + 1, end);                                            \
    }

IMPL_DIRECT_SORT(direct, MainMemory);
IMPL_DIRECT_SORT(direct_import, MainMemoryImport);

#undef IMPL_DIRECT_SORT

void sort_array(MainMemory::ptr_type begin_ptr, const int32_t length) {
    int32_t *arr = new int32_t[length];
    MainMemory::mts_memcpy(arr, begin_ptr, length * sizeof(int32_t));
    quicksort(arr, 0, length - 1);
    MainMemory::stm_memcpy(begin_ptr, arr, length * sizeof(int32_t));
    delete[] arr;
}

void sort_array_fallback(MainMemory::ptr_type begin_ptr, int32_t length) {
    int32_t *arr = new int32_t[length];
    MainMemory::mts_memcpy_fb(arr, begin_ptr, length * sizeof(int32_t));
    quicksort(arr, 0, length - 1);
    MainMemory::stm_memcpy_fb(begin_ptr, arr, length * sizeof(int32_t));
    delete[] arr;
}

void sort_array_import(MainMemoryImport::ptr_type begin_ptr, int32_t length) {
    int32_t *arr = new int32_t[length];
    MainMemoryImport::mts_memcpy(arr, begin_ptr, length * sizeof(int32_t));
    quicksort(arr, 0, length - 1);
    MainMemoryImport::stm_memcpy(begin_ptr, arr, length * sizeof(int32_t));
    delete[] arr;
}

void sort_array_import_fallback(MainMemoryImport::ptr_type begin_ptr, int32_t length) {
    int32_t *arr = new int32_t[length];
    MainMemoryImport::mts_memcpy(arr, begin_ptr, length * sizeof(int32_t));
    quicksort(arr, 0, length - 1);
    MainMemoryImport::stm_memcpy(begin_ptr, arr, length * sizeof(int32_t));
    delete[] arr;
}

void sort_array_direct(MainMemory::ptr_type begin_ptr, const int32_t length) {
    quicksort_direct(begin_ptr, 0, length - 1);
}

void sort_array_direct_import(MainMemoryImport::ptr_type begin_ptr, const int32_t length) {
    quicksort_direct_import(begin_ptr, 0, length - 1);
}

void init_marshalling(const rb_bridge_func rb_bridge, const uintptr_t self_id) {
    MainMemory::init(rb_bridge, self_id);
}
