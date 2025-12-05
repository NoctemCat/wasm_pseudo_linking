// #define NoGCT
// #define NoInline
// #define NoInlineImport
#if MEMORY_TESTS
global using unsafe MainMemoryPtr = void*;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: DisableRuntimeMarshalling]
namespace WasmPseudoLinking;

public struct AnsiString(string text) : IDisposable
{
    nint ptr = Marshal.StringToHGlobalAnsi(text);

    public void Dispose()
    {
        if (ptr != nint.Zero)
        {
            Marshal.FreeHGlobal(ptr);
            ptr = nint.Zero;
        }
    }

    public readonly unsafe void* GetPtr() => (void*)ptr;
}


[SkipLocalsInit]
internal unsafe static class MainMemory
{
    static delegate* unmanaged
#if !NoGCT
 [SuppressGCTransition]
#endif
    <MainMemoryPtr, int, void> setI32 = null;
    static delegate* unmanaged
#if !NoGCT
 [SuppressGCTransition]
#endif
    <MainMemoryPtr, int> getI32 = null;
    static delegate* unmanaged<void*, MainMemoryPtr> stmAddFnPtr = null;
    static delegate* unmanaged
#if !NoGCT
 [SuppressGCTransition]
#endif
    <MainMemoryPtr, void*, int, void> stmMemcpy = null;
    static delegate* unmanaged
#if !NoGCT
 [SuppressGCTransition]
#endif
    <void*, MainMemoryPtr, int, void> mtsMemcpy = null;
    static delegate* unmanaged
#if !NoGCT
 [SuppressGCTransition]
#endif
    <MainMemoryPtr, void*, int, void> stmMemcpyFb = null;
    static delegate* unmanaged
#if !NoGCT
 [SuppressGCTransition]
#endif
    <void*, MainMemoryPtr, int, void> mtsMemcpyFb = null;

#if !NoInline
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static void SetI32(MainMemoryPtr addr, int value) => setI32(addr, value);
#if !NoInline
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static void SetI32(MainMemoryPtr addr, int offset, int value) => setI32((byte*)addr + (sizeof(int) * offset), value);
#if !NoInline
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static int GetI32(MainMemoryPtr addr) => getI32(addr);
#if !NoInline
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static int GetI32(MainMemoryPtr addr, int offset) => getI32((byte*)addr + (sizeof(int) * offset));

#if !NoInline
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static MainMemoryPtr StmAddFnPtr(void* addr) => stmAddFnPtr(addr);

#if !NoInline
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static void StmMemcpy(MainMemoryPtr dest, void* src, int size) => stmMemcpy(dest, src, size);
#if !NoInline
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static void MtsMemcpy(void* dest, MainMemoryPtr src, int size) => mtsMemcpy(dest, src, size);
#if !NoInline
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static void StmMemcpyFb(MainMemoryPtr dest, void* src, int size) => stmMemcpyFb(dest, src, size);
#if !NoInline
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static void MtsMemcpyFb(void* dest, MainMemoryPtr src, int size) => mtsMemcpyFb(dest, src, size);

    public static void Init(delegate* unmanaged<nint, void*, void*> rbBridgeFunc, nint selfId)
    {
        var rbBridgeFallback = (delegate* unmanaged<nint, void*, void*>)LoadBridgeFunc(selfId, "rbBridgeFb");

        setI32 = (delegate* unmanaged
#if !NoGCT
     [SuppressGCTransition]
#endif
        <MainMemoryPtr, int, void>)LoadBridgeFunc(nint.Zero, "setI32");
        getI32 = (delegate* unmanaged
#if !NoGCT
     [SuppressGCTransition]
#endif
        <MainMemoryPtr, int>)LoadBridgeFunc(nint.Zero, "getI32");
        stmAddFnPtr = (delegate* unmanaged<void*, MainMemoryPtr>)LoadBridgeFunc(selfId, "stm_addFnPtr");
        stmMemcpy = (delegate* unmanaged
#if !NoGCT
     [SuppressGCTransition]
#endif
        <MainMemoryPtr, void*, int, void>)LoadBridgeFunc(selfId, "stm_memcpy");
        mtsMemcpy = (delegate* unmanaged
#if !NoGCT
     [SuppressGCTransition]
#endif
        <void*, MainMemoryPtr, int, void>)LoadBridgeFunc(selfId, "mts_memcpy");
        stmMemcpyFb = (delegate* unmanaged
#if !NoGCT
     [SuppressGCTransition]
#endif
        <MainMemoryPtr, void*, int, void>)LoadBridgeFbFunc(selfId, "stm_memcpy");
        mtsMemcpyFb = (delegate* unmanaged
#if !NoGCT
     [SuppressGCTransition]
#endif
        <void*, MainMemoryPtr, int, void>)LoadBridgeFbFunc(selfId, "mts_memcpy");

        void* LoadBridgeFunc(nint libId, string name)
        {
            using AnsiString setStr = new(name);
            return rbBridgeFunc(libId, setStr.GetPtr());
        }
        void* LoadBridgeFbFunc(nint libId, string name)
        {
            using AnsiString setStr = new(name);
            return rbBridgeFallback(libId, setStr.GetPtr());
        }
    }
}

#if !RB_DISABLE_IMPORT
// If "WasmImportLinkage" is not present, you can add it manually with
// <NativeFileReference Include="imports.c" />
// "imports.c" needs "main_setI32" declaration inside here
// and then import it by 
// [DllImport("imports", EntryPoint = "main_setI32")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time",
    Justification = "For some reason 'LibraryImport' fails with <RunAOTCompilation>true</RunAOTCompilation>"
)]
#if false
internal unsafe static class MainMemoryImport
{
    [DllImport("env", EntryPoint = "main_setI32")]
    [WasmImportLinkage]
#if !NoGCT
    [SuppressGCTransition]
#endif
    public static extern void SetI32(MainMemoryPtr addr, int value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetI32(MainMemoryPtr addr, int offset, int value)
        => SetI32((byte*)addr + (sizeof(int) * offset), value);

    [DllImport("env", EntryPoint = "main_getI32")]
    [WasmImportLinkage]
#if !NoGCT
    [SuppressGCTransition]
#endif
    public static extern int GetI32(MainMemoryPtr addr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetI32(MainMemoryPtr addr, int offset)
        => GetI32((byte*)addr + (sizeof(int) * offset));

    [DllImport("env", EntryPoint = "mts_addFnPtr")]
    [WasmImportLinkage]
    public static extern void* MtsAddFnPtr(MainMemoryPtr addr);

    [DllImport("env", EntryPoint = "stm_addFnPtr")]
    [WasmImportLinkage]
    public static extern MainMemoryPtr StmAddFnPtr(void* addr);

    [DllImport("env", EntryPoint = "mts_memcpy")]
    [WasmImportLinkage]

#if !NoGCT
    [SuppressGCTransition]
#endif
    public static extern void MtsMemcpy(void* dest, MainMemoryPtr src, int size);

    [DllImport("env", EntryPoint = "stm_memcpy")]
    [WasmImportLinkage]

#if !NoGCT
    [SuppressGCTransition]
#endif
    public static extern void StmMemcpy(MainMemoryPtr dest, void* src, int size);

    [DllImport("env", EntryPoint = "mts_memcpy_fb")]
    [WasmImportLinkage]
#if !NoGCT
    [SuppressGCTransition]
#endif
    public static extern void MtsMemcpyFb(void* dest, MainMemoryPtr src, int size);

    [DllImport("env", EntryPoint = "stm_memcpy_fb")]
    [WasmImportLinkage]
#if !NoGCT
    [SuppressGCTransition]
#endif
    public static extern void StmMemcpyFb(MainMemoryPtr dest, void* src, int size);
}
#else
internal unsafe static class MainMemoryImport
{
#if !NoInlineImport
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static void SetI32(MainMemoryPtr addr, int value)
    {
        main_setI32(addr, value);

        [DllImport("env")]
        [WasmImportLinkage]
#if !NoGCT
        [SuppressGCTransition]
#endif
        static extern void main_setI32(MainMemoryPtr addr, int value);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetI32(MainMemoryPtr addr, int offset, int value)
        => SetI32((byte*)addr + (sizeof(int) * offset), value);

#if !NoInlineImport
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static int GetI32(MainMemoryPtr addr)
    {
        return main_getI32(addr);

        [DllImport("env")]
        [WasmImportLinkage]
#if !NoGCT
        [SuppressGCTransition]
#endif
        static extern int main_getI32(MainMemoryPtr addr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetI32(MainMemoryPtr addr, int offset)
        => GetI32((byte*)addr + (sizeof(int) * offset));

    [DllImport("env", EntryPoint = "mts_addFnPtr")]
    [WasmImportLinkage]
    public static extern void* MtsAddFnPtr(MainMemoryPtr addr);

    [DllImport("env", EntryPoint = "stm_addFnPtr")]
    [WasmImportLinkage]
    public static extern MainMemoryPtr StmAddFnPtr(void* addr);

#if !NoInlineImport
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static void MtsMemcpy(void* dest, MainMemoryPtr src, int size)
    {
        mts_memcpy(dest, src, size);

        [DllImport("env")]
        [WasmImportLinkage]
#if !NoGCT
        [SuppressGCTransition]
#endif
        static extern void mts_memcpy(void* dest, MainMemoryPtr src, int size);
    }

#if !NoInlineImport
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static void StmMemcpy(MainMemoryPtr dest, void* src, int size)
    {
        stm_memcpy(dest, src, size);

        [DllImport("env")]
        [WasmImportLinkage]
#if !NoGCT
        [SuppressGCTransition]
#endif
        static extern void stm_memcpy(MainMemoryPtr dest, void* src, int size);
    }

#if !NoInlineImport
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static void MtsMemcpyFb(void* dest, MainMemoryPtr src, int size)
    {
        mts_memcpy_fb(dest, src, size);

        [DllImport("env")]
        [WasmImportLinkage]
#if !NoGCT
        [SuppressGCTransition]
#endif
        static extern void mts_memcpy_fb(void* dest, MainMemoryPtr src, int size);
    }

#if !NoInlineImport
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static void StmMemcpyFb(MainMemoryPtr dest, void* src, int size)
    {
        stm_memcpy_fb(dest, src, size);

        [DllImport("env")]
        [WasmImportLinkage]
#if !NoGCT
        [SuppressGCTransition]
#endif
        static extern void stm_memcpy_fb(MainMemoryPtr dest, void* src, int size);
    }
}
#endif
#endif

#endif