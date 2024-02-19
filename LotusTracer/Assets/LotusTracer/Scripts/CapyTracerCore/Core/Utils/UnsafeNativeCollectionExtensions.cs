using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace CapyTracerCore.Core
{
    public static unsafe class UnsafeNativeCollectionExtensions
    {
        public static ref T GetUnsafeReadOnlyRef<T>(this NativeArray<T> array, int index) where T : unmanaged
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafeReadOnlyPtr(), index);
        }
    
        public static ref T GetUnsafeRef<T>(this NativeArray<T> array, int index) where T : unmanaged
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
        }
    
        public static void SetUnsafeRef<T>(this NativeArray<T> array, int index, T value) where T : unmanaged
        {
            UnsafeUtility.WriteArrayElement(array.GetUnsafePtr(), index, value);
        }
    
        public static ref T GetUnsafeReadOnlyRef<T>(this NativeList<T> list, int index) where T : unmanaged
        {
            void* listPtr = NativeListUnsafeUtility.GetUnsafeReadOnlyPtr(list);

            IntPtr startPtr = (IntPtr)listPtr;
            int offsetPtr = index * sizeof(T);
        
            IntPtr elementPtr = IntPtr.Add(startPtr,  offsetPtr);

            return ref UnsafeUtility.AsRef<T>(elementPtr.ToPointer());
        }
    
        public static unsafe void UnsafeWrite<T>(this NativeList<T> list, int index, T value) where T : unmanaged
        {
            void* listPtr = NativeListUnsafeUtility.GetUnsafePtr(list);
    
            IntPtr startPtr = (IntPtr)listPtr;
            int offsetPtr = index * Marshal.SizeOf<T>();
    
            IntPtr elementPtr = IntPtr.Add(startPtr, offsetPtr);
    
            *(T*)elementPtr = value;
        }
    }
}