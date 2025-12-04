global using unsafe MainMemoryPtr = void*;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
    static delegate* unmanaged[Cdecl]<MainMemoryPtr, int, void> setI32 = null;
    static delegate* unmanaged[Cdecl]<MainMemoryPtr, int> getI32 = null;
    static delegate* unmanaged[Cdecl]<void*, MainMemoryPtr> stmAddFnPtr = null;
    static delegate* unmanaged[Cdecl]<MainMemoryPtr, void*, int, void> stmMemcpy = null;
    static delegate* unmanaged[Cdecl]<void*, MainMemoryPtr, int, void> mtsMemcpy = null;
    static delegate* unmanaged[Cdecl]<MainMemoryPtr, void*, int, void> stmMemcpyFb = null;
    static delegate* unmanaged[Cdecl]<void*, MainMemoryPtr, int, void> mtsMemcpyFb = null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetI32(MainMemoryPtr addr, int value) => setI32(addr, value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetI32(MainMemoryPtr addr, int offset, int value) => setI32((byte*)addr + (sizeof(int) * offset), value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetI32(MainMemoryPtr addr) => getI32(addr);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetI32(MainMemoryPtr addr, int offset) => getI32((byte*)addr + (sizeof(int) * offset));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MainMemoryPtr StmAddFnPtr(void* addr) => stmAddFnPtr(addr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void StmMemcpy(MainMemoryPtr dest, void* src, int size) => stmMemcpy(dest, src, size);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MtsMemcpy(void* dest, MainMemoryPtr src, int size) => mtsMemcpy(dest, src, size);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void StmMemcpyFb(MainMemoryPtr dest, void* src, int size) => stmMemcpyFb(dest, src, size);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MtsMemcpyFb(void* dest, MainMemoryPtr src, int size) => mtsMemcpyFb(dest, src, size);

    public static void Init(delegate* unmanaged[Cdecl]<nint, void*, void*> rbBridgeFunc, nint selfId)
    {
        var rbBridgeFallback = (delegate* unmanaged[Cdecl]<nint, void*, void*>)LoadBridgeFunc(selfId, "rbBridgeFb");

        setI32 = (delegate* unmanaged[Cdecl]<MainMemoryPtr, int, void>)LoadBridgeFunc(nint.Zero, "setI32");
        getI32 = (delegate* unmanaged[Cdecl]<MainMemoryPtr, int>)LoadBridgeFunc(nint.Zero, "getI32");
        stmAddFnPtr = (delegate* unmanaged[Cdecl]<void*, MainMemoryPtr>)LoadBridgeFunc(selfId, "stm_addFnPtr");
        stmMemcpy = (delegate* unmanaged[Cdecl]<MainMemoryPtr, void*, int, void>)LoadBridgeFunc(selfId, "stm_memcpy");
        mtsMemcpy = (delegate* unmanaged[Cdecl]<void*, MainMemoryPtr, int, void>)LoadBridgeFunc(selfId, "mts_memcpy");
        stmMemcpyFb = (delegate* unmanaged[Cdecl]<MainMemoryPtr, void*, int, void>)LoadBridgeFbFunc(selfId, "stm_memcpy");
        mtsMemcpyFb = (delegate* unmanaged[Cdecl]<void*, MainMemoryPtr, int, void>)LoadBridgeFbFunc(selfId, "mts_memcpy");

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
// [DllImport("imports", EntryPoint = "main_setI32", CallingConvention = CallingConvention.Cdecl)]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time",
    Justification = "For some reason 'LibraryImport' fails with <RunAOTCompilation>true</RunAOTCompilation>"
)]
internal unsafe static class MainMemoryImport
{
    [DllImport("env", EntryPoint = "main_setI32", CallingConvention = CallingConvention.Cdecl)]
    [WasmImportLinkage]
    public static extern void SetI32(MainMemoryPtr addr, int value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetI32(MainMemoryPtr addr, int offset, int value)
        => SetI32((byte*)addr + (sizeof(int) * offset), value);

    [DllImport("env", EntryPoint = "main_getI32", CallingConvention = CallingConvention.Cdecl)]
    [WasmImportLinkage]
    public static extern int GetI32(MainMemoryPtr addr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetI32(MainMemoryPtr addr, int offset)
        => GetI32((byte*)addr + (sizeof(int) * offset));

    [DllImport("env", EntryPoint = "mts_addFnPtr", CallingConvention = CallingConvention.Cdecl)]
    [WasmImportLinkage]
    public static extern void* MtsAddFnPtr(MainMemoryPtr addr);

    [DllImport("env", EntryPoint = "stm_addFnPtr", CallingConvention = CallingConvention.Cdecl)]
    [WasmImportLinkage]
    public static extern MainMemoryPtr StmAddFnPtr(void* addr);

    [DllImport("env", EntryPoint = "mts_memcpy", CallingConvention = CallingConvention.Cdecl)]
    [WasmImportLinkage]
    public static extern void MtsMemcpy(void* dest, MainMemoryPtr src, int size);

    [DllImport("env", EntryPoint = "stm_memcpy", CallingConvention = CallingConvention.Cdecl)]
    [WasmImportLinkage]
    public static extern void StmMemcpy(MainMemoryPtr dest, void* src, int size);

    [DllImport("env", EntryPoint = "mts_memcpy_fb", CallingConvention = CallingConvention.Cdecl)]
    [WasmImportLinkage]
    public static extern void MtsMemcpyFb(void* dest, MainMemoryPtr src, int size);

    [DllImport("env", EntryPoint = "stm_memcpy_fb", CallingConvention = CallingConvention.Cdecl)]
    [WasmImportLinkage]
    public static extern void StmMemcpyFb(MainMemoryPtr dest, void* src, int size);
}
#endif

