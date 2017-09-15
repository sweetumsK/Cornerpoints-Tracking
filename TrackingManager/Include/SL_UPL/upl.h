#ifndef __UPL_H_INCLUDED
#define __UPL_H_INCLUDED

#if defined(_MSC_VER)
#if !defined(UPL_IGNORE_PRAGMA_LIB) && !defined(UPL_EXPORTS)
#if _MSC_VER < 1700		// after VS2012, don't use pragma to add additional lib or it'll increase dependency within the solutions.
	#pragma comment(lib, "upl.lib")
#endif
#endif
#endif

#include "upldefs.h"
#include "uplcpuinf.h"


#ifdef __cplusplus
extern "C"
{
#endif

///////////////////////////////////////////////////////////////////////////////////////////////
// UPL and Enhanced IPP functions

// multi-threaded
UPLAPI(UIppStatus, uippiResize_8u_C3R, (const UInt8* pSrc, RectSizeInt srcSize, Int srcStep, RectInt srcRoi,
                                      UInt8* pDst, Int dstStep, RectSizeInt dstRoiSize,
                                      Double xFactor, Double yFactor, Int interpolation))

// multi-threaded
UPLAPI(UIppStatus, uippiResize_8u_C1R, (const UInt8* pSrc, RectSizeInt srcSize, SizeInt srcStep, RectInt srcRoi,
                                      UInt8* pDst, SizeInt dstStep, RectSizeInt dstRoiSize,
                                      Double xFactor, Double yFactor, Int interpolation))

// multi-threaded
UPLAPI(UIppStatus, uippiRGBToYCbCr422_8u_C3C2R,(const UInt8* pSrc, SizeInt srcStep ,
       UInt8* pDst, SizeInt dstStep, RectSizeInt roiSize))

// multi-threaded
UPLAPI(UIppStatus, uippiBGRToYCbCr420_8u_C3P3R,   (const UInt8*  pSrc, SizeInt srcStep, UInt8* pDst[3], SizeInt dstStep[3], RectSizeInt roiSize ))

// multi-threaded
UPLAPI(UIppStatus, uippiRGBToYCbCr_8u_C3R,(const UInt8* pSrc, SizeInt srcStep, UInt8* pDst, SizeInt dstStep, RectSizeInt roiSize))

// multi-threaded
UPLAPI(UIppStatus, uippiYCbCrToRGB_8u_C3R,(const UInt8* pSrc, SizeInt srcStep, UInt8* pDst, SizeInt dstStep, RectSizeInt roiSize))

UPLAPI(UplResult, __uplDeinterlaceImage_8u_CXR, (int x, const UInt8* pSrc, SizePtr srcStep, UInt8* pDst, SizePtr dstStep, RectSizePtr roiSize, Int method))

UPLAPI(UplResult, uplDeinterlaceImage_8u_C4R, (const UInt8* pSrc, SizePtr srcStep, UInt8* pDst, SizePtr dstStep, RectSizePtr roiSize, Int method))
// multi-threaded
UPLAPI(UplResult, uplDeinterlaceImage_8u_C3R, (const UInt8* pSrc, SizePtr srcStep, UInt8* pDst, SizePtr dstStep, RectSizePtr roiSize, Int method))
// multi-threaded
UPLAPI(UplResult, uplDeinterlaceImage_8u_C3IR, (UInt8* pSrcDst, SizePtr srcDstStep, RectSizePtr roiSize, Int method))
// multi-threaded
UPLAPI(UplResult, uplDeinterlaceImage_8u_C1R, (const UInt8* pSrc, SizePtr srcStep, UInt8* pDst, SizePtr dstStep, RectSizePtr roiSize, Int method))
// multi-threaded
UPLAPI(UplResult, uplDeinterlaceImageBGR565_16u_C3R, (const UInt16* pSrc, SizePtr srcStep, UInt16* pDst, SizePtr dstStep, RectSizePtr roiSize, Int method))
// multi-threaded
UPLAPI(UplResult, uplDeinterlaceImageBGR555_16u_C3R, (const UInt16* pSrc, SizePtr srcStep, UInt16* pDst, SizePtr dstStep, RectSizePtr roiSize, Int method))
// multi-threaded
UPLAPI(UplResult, uplDeinterlaceFrame_8u_C3R, (const UInt8* pSrc, SizeInt srcStep, UInt8* pDst, SizeInt dstStep, RectSizeInt roiSize, UplDeinterlaceFrameMethod method))
// multi-threaded
UPLAPI(UplResult, uplDeinterlaceFrame_8u_C1R, (const UInt8* pSrc, SizeInt srcStep, UInt8* pDst, SizeInt dstStep, RectSizeInt roiSize, UplDeinterlaceFrameMethod method))



///////////////////////////////////////////////////////////////////////////////////////////
// uplCopyMemory
//

UPLAPI( Void *, uplCopyMemory, ( Void * pDst, const Void * pSrc, SizePtr size ))

///////////////////////////////////////////////////////////////////////////////////////////
// uplCopyMemoryEx
//
// Used for copy large memory block.
// Minimize L1/L2/L3 cache allocated during copy memory.
// It can also be used in copy memory from system memory to video memory.

UPLAPI( Void *, uplCopyMemoryEx, ( Void * pDst, const Void * pSrc, SizePtr size, Int mode ))


/////////////////////////////////////////////////////////////////////////////////////////////////////////
// Deprecated old functions

#if !defined(OLD_UPLAPI)

#if defined( _MSC_VER )
    #define OLD_UPLAPI(type,name,arg)		typedef type (UPL_STDCALL* name##FuncType) arg; \
											extern __declspec(dllimport) name##FuncType name;
#else
    #define OLD_UPLAPI( type,name,arg )     typedef type (UPL_STDCALL* name##FuncType) arg; \
											extern 	name##FuncType name;
#endif

#endif


// Deprecated.
OLD_UPLAPI(UplResult, uippiDeinterlace_8u_C1R, (const UInt8* pSrc, SizeInt srcStep, UInt8* pDst, SizeInt dstStep, RectSizeInt roiSize, UippDeInterlaceMethod method))
OLD_UPLAPI(UplResult, uippiDeinterlace_8u_C3R, (const UInt8* pSrc, SizeInt srcStep, UInt8* pDst, SizeInt dstStep, RectSizeInt roiSize, UippDeInterlaceMethod method))

// Deprecated.
//#if defined(_MSC_VER) && (_MSC_VER <= 1200)
OLD_UPLAPI(Boolean, uplDeinterlace, (Void* pPakDibSrc, Void* ppPakDibDest, UInt32 dwFrameType))
OLD_UPLAPI(Boolean, uplConvertMaskBitCount_8_24, (UInt8* pPixSrc, Void* pBihSrc, UInt8* pPixDst, Void* pBihDst))
UPL_IMPORT Int uplBGRtoYUV2_420(Void* pbihSrc, Void* pData, Void* pDest);
//#endif

#ifdef __cplusplus
}
#endif


#endif

