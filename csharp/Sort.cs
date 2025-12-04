using System.Buffers;
using System.Runtime.CompilerServices;

namespace WasmPseudoLinking;

[SkipLocalsInit]
public unsafe static class Sort
{
    public static void Quicksort(scoped Span<int> array, int start, int end)
    {
        if (start >= end)
            return;
        int pivot = array[end];
        int left = 0;
        int right = 0;
        while (left + right < end - start)
        {
            int num = array[start + left];
            if (num < pivot)
            {
                left++;
            }
            else
            {
                array[start + left] = array[end - right - 1];
                array[end - right - 1] = pivot;
                array[end - right] = num;
                right++;
            }
        }
        Quicksort(array, start, start + left - 1);
        Quicksort(array, start + left + 1, end);
    }


    public static void QuicksortDirect(MainMemoryPtr array, int start, int end)
    {
        if (start >= end)
            return;
        int pivot = MainMemory.GetI32(array, end);
        int left = 0;
        int right = 0;
        while (left + right < end - start)
        {
            int num = MainMemory.GetI32(array, start + left);
            if (num < pivot)
            {
                left++;
            }
            else
            {
                MainMemory.SetI32(array, start + left, MainMemory.GetI32(array, end - right - 1));
                MainMemory.SetI32(array, end - right - 1, pivot);
                MainMemory.SetI32(array, end - right, num);
                right++;
            }
        }
        QuicksortDirect(array, start, start + left - 1);
        QuicksortDirect(array, start + left + 1, end);
    }

#if !RB_DISABLE_IMPORT
    public static void QuicksortDirectImport(MainMemoryPtr array, int start, int end)
    {
        if (start >= end)
            return;
        int pivot = MainMemoryImport.GetI32(array, end);
        int left = 0;
        int right = 0;
        while (left + right < end - start)
        {
            int num = MainMemoryImport.GetI32(array, start + left);
            if (num < pivot)
            {
                left++;
            }
            else
            {
                MainMemoryImport.SetI32(array, start + left, MainMemoryImport.GetI32(array, end - right - 1));
                MainMemoryImport.SetI32(array, end - right - 1, pivot);
                MainMemoryImport.SetI32(array, end - right, num);
                right++;
            }
        }
        QuicksortDirectImport(array, start, start + left - 1);
        QuicksortDirectImport(array, start + left + 1, end);
    }
#endif
}
