using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
[assembly: System.Runtime.Versioning.SupportedOSPlatform("browser")]

namespace WasmPseudoLinking;

internal partial class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }

    [JSExport]
    public unsafe static nint EntryPoint(MainMemoryPtr rbBridgeFunc, nint selfId)
    {
        Console.WriteLine($"dotnet: EntryPoint entered {(nint)rbBridgeFunc}, id:{selfId}");
        MainMemory.Init((delegate* unmanaged<nint, void*, void*>)rbBridgeFunc, selfId);
        return (nint)MainMemory.StmAddFnPtr((delegate* unmanaged<void*, MainMemoryPtr>)&NetDlsym);
    }

    [UnmanagedCallersOnly]
    public unsafe static MainMemoryPtr NetDlsym(void* namePtr)
    {
        string? name = Marshal.PtrToStringUTF8((nint)namePtr);
        Console.WriteLine($"dotnet: dlsym \"{name}\"");

        return name switch
        {
            nameof(Sub) => MainMemory.StmAddFnPtr((delegate* unmanaged<MainMemoryPtr, MainMemoryPtr, MainMemoryPtr, void>)&Sub),
            nameof(SortArray) => MainMemory.StmAddFnPtr((delegate* unmanaged<MainMemoryPtr, int, void>)&SortArray),
            nameof(SortArrayFallback) => MainMemory.StmAddFnPtr((delegate* unmanaged<MainMemoryPtr, int, void>)&SortArrayFallback),
            nameof(SortArrayDirect) => MainMemory.StmAddFnPtr((delegate* unmanaged<MainMemoryPtr, int, void>)&SortArrayDirect),
#if !RB_DISABLE_IMPORT
            nameof(SortArrayImport) => MainMemory.StmAddFnPtr((delegate* unmanaged<MainMemoryPtr, int, void>)&SortArrayImport),
            nameof(SortArrayImportFallback) => MainMemory.StmAddFnPtr((delegate* unmanaged<MainMemoryPtr, int, void>)&SortArrayImportFallback),
            nameof(SortArrayDirectImport) => MainMemory.StmAddFnPtr((delegate* unmanaged<MainMemoryPtr, int, void>)&SortArrayDirectImport),
#endif
            _ => null,
        };
    }

    [UnmanagedCallersOnly]
    [SkipLocalsInit]
    public unsafe static void Sub(MainMemoryPtr aPtr, MainMemoryPtr bPtr, MainMemoryPtr retPtr)
    {
        MainMemory.SetI32(retPtr, MainMemory.GetI32(aPtr) - MainMemory.GetI32(bPtr));
    }

    [UnmanagedCallersOnly]
    [SkipLocalsInit]
    public unsafe static void SortArray(MainMemoryPtr array, int length)
    {
        Span<int> span = GC.AllocateUninitializedArray<int>(length, true);
        fixed (void* spanPtr = span)
        {
            MainMemory.MtsMemcpy(spanPtr, array, sizeof(int) * length);
            Sort.Quicksort(span, 0, length - 1);
            MainMemory.StmMemcpy(array, spanPtr, sizeof(int) * length);
        }
    }

    [UnmanagedCallersOnly]
    [SkipLocalsInit]
    public unsafe static void SortArrayFallback(MainMemoryPtr array, int length)
    {
        Span<int> span = GC.AllocateUninitializedArray<int>(length, true);
        fixed (void* spanPtr = span)
        {
            MainMemory.MtsMemcpyFb(spanPtr, array, sizeof(int) * length);
            Sort.Quicksort(span, 0, length - 1);
            MainMemory.StmMemcpyFb(array, spanPtr, sizeof(int) * length);
        }
    }

    [UnmanagedCallersOnly]
    [SkipLocalsInit]
    public unsafe static void SortArrayDirect(MainMemoryPtr array, int length)
    {
        Sort.QuicksortDirect(array, 0, length - 1);
    }

#if !RB_DISABLE_IMPORT
    [UnmanagedCallersOnly]
    [SkipLocalsInit]
    public unsafe static void SortArrayImport(MainMemoryPtr array, int length)
    {
        Span<int> span = GC.AllocateUninitializedArray<int>(length, true);
        fixed (void* spanPtr = span)
        {
            MainMemoryImport.MtsMemcpy(spanPtr, array, sizeof(int) * length);
            Sort.Quicksort(span, 0, length - 1);
            MainMemoryImport.StmMemcpy(array, spanPtr, sizeof(int) * length);
        }
    }

    [UnmanagedCallersOnly]
    [SkipLocalsInit]
    public unsafe static void SortArrayImportFallback(MainMemoryPtr array, int length)
    {
        Span<int> span = GC.AllocateUninitializedArray<int>(length, true);
        fixed (void* spanPtr = span)
        {
            MainMemoryImport.MtsMemcpyFb(spanPtr, array, sizeof(int) * length);
            Sort.Quicksort(span, 0, length - 1);
            MainMemoryImport.StmMemcpyFb(array, spanPtr, sizeof(int) * length);
        }
    }


    [UnmanagedCallersOnly]
    [SkipLocalsInit]
    public unsafe static void SortArrayDirectImport(MainMemoryPtr array, int length)
    {
        Sort.QuicksortDirectImport(array, 0, length - 1);
    }
#endif
}


