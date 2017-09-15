#ifndef __UNITYPES_H_INCLUDED
#define __UNITYPES_H_INCLUDED

typedef void Void;

#if defined(__cplusplus)
typedef bool Boolean;
#else
typedef int Boolean;
#endif

#if defined(_MSC_VER)
	#if defined(_M_AMD64)
		// does not follow Windows/Linux
		typedef __int64 Integer;			
		typedef unsigned __int64 UnsignedInteger;
	#else
		typedef int Integer;
		typedef unsigned int UnsignedInteger;
	#endif

#else
#error Not ready
#endif


typedef unsigned char   UInt8;
typedef unsigned short  UInt16;
typedef unsigned int    UInt32;

typedef signed char    Int8;
typedef signed short   Int16;
typedef signed int     Int32;

typedef __int64				Int64;
typedef unsigned __int64	UInt64;

typedef Int32	SizeInt32;
typedef UInt32	SizeUInt32;

typedef float   Float32;
typedef double  Double64;


typedef struct {
	Int32 width;
	Int32 height;
} RectSizeInt32;

typedef struct {
    Int32 x;
    Int32 y;
    Int32 width;
    Int32 height;
} RectInt32;

//
typedef UInt8 Byte;

typedef Integer Int;
typedef UnsignedInteger UInt;

typedef Float32   Float;
typedef Double64  Double;

typedef Int64		LongInt;
typedef UInt64		LongUInt;

typedef Int	SizeInt;


#if defined(_MSC_VER)
typedef __int64 Int64;
typedef unsigned __int64 UInt64;
#else
typedef long long Int64;
typedef unsigned long long UInt64;
#endif



#if defined(_MSC_VER)
#if defined(_M_AMD64)
typedef Int64 IntPtrPrecision;
typedef UInt64 UIntPtrPrecision;
#else
typedef Int32  IntPtrPrecision;
typedef UInt32 UIntPtrPrecision;
#endif
#else
#error Not Ready
#endif

typedef IntPtrPrecision IntPtrPrec;
typedef UIntPtrPrecision UIntPtrPrec;

typedef IntPtrPrec		IntPtr;
typedef UIntPtrPrec		UIntPtr;

#if defined(_MSC_VER)
	#if defined(_M_AMD64)
	typedef	UInt64  SizeUInt;
	typedef UInt64	Size_t;
	typedef UInt64	SizeT;

	typedef UInt64	SizePtr;
	typedef UInt64	SSize_t;
	#else
	typedef UInt32	SizeUInt;
	typedef UInt32	Size_t;
	typedef UInt32	SizeT;

	typedef UInt32	SizePtr;
	typedef UInt32	SSize_t;
	#endif
#else
#error not ready
#endif

typedef struct {
	Int width;
	Int height;
} RectSizeInt;

typedef struct {
	SizePtr width;
	SizePtr height;
} RectSizePtr;

typedef struct {
    Int x;
    Int y;
    Int width;
    Int height;
} RectInt;

typedef RectInt	Rect;

typedef struct {
    IntPtr x;
    IntPtr y;
    IntPtr width;
    IntPtr height;
} RectPtr;


typedef struct {
    Int x;
    Int y;
} PointInt;

typedef PointInt Point;

typedef RectSizeInt Size;

 
#if defined(_MSC_VER)
	#if defined(_M_AMD64)
	typedef Int64 HResult;
	typedef Int64 HStatus;
	#else
	typedef long HResult;
	typedef int  HStatus;
	#endif
#else
	// TODO:
	typedef long HResult;
	typedef int HStatus;
#endif


#endif