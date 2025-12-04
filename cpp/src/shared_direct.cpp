#include "shared_direct.hpp"
#include <cstdint>

void sub(const int32_t *a, const int32_t *b, int32_t *ret) { *ret = *a - *b; }

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

void sort_array(int32_t *begin, const int32_t length) { quicksort(begin, 0, length - 1); }