

#include "main_sort.hpp"
#include "timer.hpp"
#include <cstddef>
#include <cstdint>
#include <emscripten/em_macros.h>
#include <iomanip>
#include <iostream>
#include <limits>
#include <random>

sort_array_fn sort_array = nullptr;
sort_array_rbfn sort_array_rb = nullptr;
sort_array_rbfn sort_array_rb_fallback = nullptr;
sort_array_rbfn sort_array_rb_import = nullptr;
sort_array_rbfn sort_array_rb_import_fallback = nullptr;
sort_array_rbfn sort_array_rb_direct = nullptr;
sort_array_rbfn sort_array_rb_direct_import = nullptr;
sort_array_rbfn sort_array_net = nullptr;
sort_array_rbfn sort_array_net_fallback = nullptr;
sort_array_rbfn sort_array_net_import = nullptr;
sort_array_rbfn sort_array_net_import_fallback = nullptr;
sort_array_rbfn sort_array_net_direct = nullptr;
sort_array_rbfn sort_array_net_direct_import = nullptr;

int32_t get_random() {
    const int min_value = std::numeric_limits<int32_t>::min();
    const int max_value = std::numeric_limits<int32_t>::max();

    static std::mt19937 prng(std::random_device{}());
    static std::uniform_int_distribution<int32_t> dist(min_value, max_value);
    return dist(prng);
}

bool validate_sort_equality(int32_t length, int32_t variation, bool silent) {
    const bool printRes = !silent;
    std::vector<int32_t> original(length);
    for (int32_t &num : original) {
        num = get_random() % variation;
    }
    const int32_t width = 28;
    if (printRes) {
        std::cout << std::setw(width) << "Generated  " << original << std::endl;
    }
#define CHECK_SORT(sort_type)                                                                      \
    std::vector<int32_t> arr_##sort_type(original);                                                \
    if (printRes) {                                                                                \
        std::cout << std::setw(width) << "Check(" #sort_type "): " << arr_##sort_type              \
                  << std::endl;                                                                    \
    }                                                                                              \
    sort_array_##sort_type(arr_##sort_type.data(), arr_##sort_type.size());                        \
    if (printRes) {                                                                                \
        std::cout << std::setw(width) << "Sort(" #sort_type "): " << arr_##sort_type << std::endl; \
    }                                                                                              \
    sorted_arrays.emplace_back(arr_##sort_type)

    std::vector<std::vector<int32_t>> sorted_arrays;

    std::vector<int32_t> arr(original);
    if (printRes) {
        std::cout << std::setw(width) << "Check: " << arr << std::endl;
    }
    sort_array(arr.data(), arr.size());
    if (printRes) {
        std::cout << std::setw(width) << "Sort: " << arr << std::endl;
    }
    sorted_arrays.emplace_back(arr);

    CHECK_SORT(rb);
    CHECK_SORT(rb_import);
    CHECK_SORT(rb_fallback);
    CHECK_SORT(rb_import_fallback);
    CHECK_SORT(rb_direct);
    CHECK_SORT(rb_direct_import);
    CHECK_SORT(net);
    CHECK_SORT(net_import);
    CHECK_SORT(net_fallback);
    CHECK_SORT(net_import_fallback);
    CHECK_SORT(net_direct);
    CHECK_SORT(net_direct_import);

    bool equal = true;
    for (int i = 0; i < length; i++) {
        for (size_t i = 0, j = 1; j < sorted_arrays.size(); i++, j++) {
            if (sorted_arrays[i] != sorted_arrays[j]) {
                equal = false;
                break;
            }
        }
        if (!equal) {
            break;
        }
    }
    if (equal) {
        std::cout << "Sorted arrays are equal" << std::endl;
    } else {
        std::cout << "Sorted arrays are not equal" << std::endl;
    }
    return equal;
#undef CHECK_SORT
}

int32_t *allocate_random_array(int32_t arr_size) {
    int32_t *arr = new int32_t[arr_size];
    for (size_t i = 0; i < arr_size; i++) {
        arr[i] = get_random();
    }
    return arr;
}

int32_t *allocate_array(int32_t arr_size) { return new int32_t[arr_size]; }

void free_array(int32_t *arr) { delete[] arr; }

double sort(const int32_t *src_arr, int32_t *dest_arr, int32_t arr_size) {
    std::memcpy(dest_arr, src_arr, arr_size * sizeof(int32_t));
    Timer timer(true);
    sort_array(dest_arr, arr_size);
    return timer.stop();
};

#define IMPL_SORT(sort_type)                                                                       \
    double sort_##sort_type(const int32_t *src_arr, int32_t *dest_arr, const int32_t arr_size) {   \
        std::memcpy(dest_arr, src_arr, arr_size * sizeof(int32_t));                                \
        Timer timer(true);                                                                         \
        sort_array_##sort_type(dest_arr, arr_size);                                                \
        return timer.stop();                                                                       \
    }

IMPL_SORT(rb);
IMPL_SORT(rb_import);
IMPL_SORT(rb_fallback);
IMPL_SORT(rb_import_fallback);
IMPL_SORT(rb_direct);
IMPL_SORT(rb_direct_import);
IMPL_SORT(net);
IMPL_SORT(net_import);
IMPL_SORT(net_fallback);
IMPL_SORT(net_import_fallback);
IMPL_SORT(net_direct);
IMPL_SORT(net_direct_import);

#undef IMPL_SORT