#ifndef __UPLDEFS_H_INCLUDED
#define __UPLDEFS_H_INCLUDED

#ifdef __cplusplus
extern "C" {
#endif

#if defined(_MSC_VER)
  #define UPL_STDCALL  __stdcall
  #define UPL_CDECL    __cdecl
  #define __Stdcall	   __stdcall
#else
  #define UPL_STDCALL
  #define UPL_CDECL
  #define __Stdcall
#endif

#if defined( _MSC_VER )
	#if !defined(UPL_EXPORTS)
		#define UPL_IMPORT	extern __declspec(dllimport)
	#else	
		#define UPL_IMPORT	__declspec(dllexport)
	#endif
#else
    #define UPL_IMPORT
#endif

#if !defined(UPLAPI)
#if defined( _MSC_VER )
    #define UPLAPI( type,name,arg )		UPL_IMPORT type UPL_STDCALL name arg;
#else
    #define UPLAPI( type,name,arg )     extern type UPL_STDCALL name arg;
#endif
#endif

#define UPL_MAJOR_VERSION 0
#define UPL_MINOR_VERSION 7

#include "unitypes.h"

typedef HResult UplResult;

enum { 
	// threading error
	uplResultThreadExecUnknownErr		= -1100,
	uplResultThreadExecNullHandle		= -1099,
	uplResultThreadExecSyncDataErr	= -1098,
	uplResultThreadExecTimeout		= -1097,
	
	uplResultError = -1, uplResultOk = 0, uplResultNoError = 0 };	// TODO:

// UIppStatus: Extend IppStatus to add threading errors
// 
typedef enum {
	// threading error
	uippStsThreadExecUnknownErr		= -1100,
	uippStsThreadExecNullHandle		= -1099,
	uippStsThreadExecSyncDataErr	= -1098,
	uippStsThreadExecTimeout		= -1097,

	//////////////////////////////////////////////////////////////////////////////
	// get from ippdef.h
     /* errors */
    uippStsNotSupportedModeErr   = -9999,  /* The requested mode is currently not supported  */
    uippStsCpuNotSupportedErr    = -9998,  /* The target cpu is not supported */

    uippStsConvergeErr            = -205, /* The algorithm does not converge*/
    uippStsSizeMatchMatrixErr     = -204, /* Unsuitable sizes of the source matrices*/
    uippStsCountMatrixErr         = -203, /* Count value is negative or equal to 0*/
    uippStsRoiShiftMatrixErr      = -202, /* RoiShift value is negative or not dividend to size of data type*/

    uippStsResizeNoOperationErr   = -201, /* One of the output image dimensions is less than 1 pixel */
    uippStsSrcDataErr             = -200, /* The source buffer contains unsupported data */
    uippStsMaxLenHuffCodeErr      = -199, /* Huff: Max length of Huffman code is more than expected one */
    uippStsCodeLenTableErr        = -198, /* Huff: Invalid codeLenTable */
    uippStsFreqTableErr           = -197, /* Huff: Invalid freqTable */

    uippStsIncompleteContextErr   = -196, /* Crypto: set up of context is'n complete */

    uippStsSingularErr            = -195, /* Matrix is singular */
    uippStsSparseErr              = -194, /* Tap positions are not in ascending order, negative or repeated*/
    uippStsBitOffsetErr           = -193, /* Incorrect bit offset value */
    uippStsQPErr                  = -192, /* Incorrect quantization parameter */
    uippStsVLCErr                 = -191, /* Illegal VLC or FLC during stream decoding */
    uippStsRegExpOptionsErr       = -190, /* RegExp: Options for pattern are incorrect */
    uippStsRegExpErr              = -189, /* RegExp: The structure pRegExpState contains wrong data */
    uippStsRegExpMatchLimitErr    = -188, /* RegExp: The match limit has been exhausted */
    uippStsRegExpQuantifierErr    = -187, /* RegExp: wrong quantifier */
    uippStsRegExpGroupingErr      = -186, /* RegExp: wrong grouping */
    uippStsRegExpBackRefErr       = -185, /* RegExp: wrong back reference */
    uippStsRegExpChClassErr       = -184, /* RegExp: wrong character class */
    uippStsRegExpMetaChErr        = -183, /* RegExp: wrong metacharacter */


    uippStsStrideMatrixErr        = -182,  /* Stride value is not positive or not dividend to size of data type */

    uippStsCTRSizeErr             = -181,  /* Wrong value for crypto CTR block size */

    uippStsJPEG2KCodeBlockIsNotAttached =-180, /* codeblock parameters are not attached to the state structure */
    uippStsNotPosDefErr           = -179,  /* Not positive-definite matrix */

    uippStsEphemeralKeyErr        = -178, /* ECC: Bad ephemeral key   */
    uippStsMessageErr                = -177, /* ECC: Bad message digest  */
    uippStsShareKeyErr            = -176, /* ECC: Invalid share key   */
    uippStsIvalidPublicKey        = -175, /* ECC: Invalid public key  */
    uippStsIvalidPrivateKey       = -174, /* ECC: Invalid private key */
    uippStsOutOfECErr             = -173, /* ECC: Point out of EC     */
    uippStsECCInvalidFlagErr      = -172, /* ECC: Invalid Flag        */

    uippStsMP3FrameHeaderErr      = -171,  /* Error in fields IppMP3FrameHeader structure */
    uippStsMP3SideInfoErr         = -170,  /* Error in fields IppMP3SideInfo structure */

    uippStsBlockStepErr           = -169,  /* Step for Block less than 8 */
    uippStsMBStepErr              = -168,  /* Step for MB less than 16 */

    uippStsAacPrgNumErr           = -167,  /* AAC: Invalid number of elements for one program   */
    uippStsAacSectCbErr           = -166,  /* AAC: Invalid section codebook                     */
    uippStsAacSfValErr            = -164,  /* AAC: Invalid scalefactor value                    */
    uippStsAacCoefValErr          = -163,  /* AAC: Invalid quantized coefficient value          */
    uippStsAacMaxSfbErr           = -162,  /* AAC: Invalid coefficient index  */
    uippStsAacPredSfbErr          = -161,  /* AAC: Invalid predicted coefficient index  */
    uippStsAacPlsDataErr          = -160,  /* AAC: Invalid pulse data attributes  */
    uippStsAacGainCtrErr          = -159,  /* AAC: Gain control not supported  */
    uippStsAacSectErr             = -158,  /* AAC: Invalid number of sections  */
    uippStsAacTnsNumFiltErr       = -157,  /* AAC: Invalid number of TNS filters  */
    uippStsAacTnsLenErr           = -156,  /* AAC: Invalid TNS region length  */
    uippStsAacTnsOrderErr         = -155,  /* AAC: Invalid order of TNS filter  */
    uippStsAacTnsCoefResErr       = -154,  /* AAC: Invalid bit-resolution for TNS filter coefficients  */
    uippStsAacTnsCoefErr          = -153,  /* AAC: Invalid TNS filter coefficients  */
    uippStsAacTnsDirectErr        = -152,  /* AAC: Invalid TNS filter direction  */
    uippStsAacTnsProfileErr       = -151,  /* AAC: Invalid TNS profile  */
    uippStsAacErr                 = -150,  /* AAC: Internal error  */
    uippStsAacBitOffsetErr        = -149,  /* AAC: Invalid current bit offset in bitstream  */
    uippStsAacAdtsSyncWordErr     = -148,  /* AAC: Invalid ADTS syncword  */
    uippStsAacSmplRateIdxErr      = -147,  /* AAC: Invalid sample rate index  */
    uippStsAacWinLenErr           = -146,  /* AAC: Invalid window length (not short or long)  */
    uippStsAacWinGrpErr           = -145,  /* AAC: Invalid number of groups for current window length  */
    uippStsAacWinSeqErr           = -144,  /* AAC: Invalid window sequence range  */
    uippStsAacComWinErr           = -143,  /* AAC: Invalid common window flag  */
    uippStsAacStereoMaskErr       = -142,  /* AAC: Invalid stereo mask  */
    uippStsAacChanErr             = -141,  /* AAC: Invalid channel number  */
    uippStsAacMonoStereoErr       = -140,  /* AAC: Invalid mono-stereo flag  */
    uippStsAacStereoLayerErr      = -139,  /* AAC: Invalid this Stereo Layer flag  */
    uippStsAacMonoLayerErr        = -138,  /* AAC: Invalid this Mono Layer flag  */
    uippStsAacScalableErr         = -137,  /* AAC: Invalid scalable object flag  */
    uippStsAacObjTypeErr          = -136,  /* AAC: Invalid audio object type  */
    uippStsAacWinShapeErr         = -135,  /* AAC: Invalid window shape  */
    uippStsAacPcmModeErr          = -134,  /* AAC: Invalid PCM output interleaving indicator  */
    uippStsVLCUsrTblHeaderErr          = -133,  /* VLC: Invalid header inside table */
    uippStsVLCUsrTblUnsupportedFmtErr  = -132,  /* VLC: Unsupported table format */
    uippStsVLCUsrTblEscAlgTypeErr      = -131,  /* VLC: Unsupported Ecs-algorithm */
    uippStsVLCUsrTblEscCodeLengthErr   = -130,  /* VLC: Incorrect Esc-code length inside table header */
    uippStsVLCUsrTblCodeLengthErr      = -129,  /* VLC: Unsupported code length inside table */
    uippStsVLCInternalTblErr           = -128,  /* VLC: Invalid internal table */
    uippStsVLCInputDataErr             = -127,  /* VLC: Invalid input data */
    uippStsVLCAACEscCodeLengthErr      = -126,  /* VLC: Invalid AAC-Esc code length */
    uippStsNoiseRangeErr         = -125,  /* Noise value for Wiener Filter is out range. */
    uippStsUnderRunErr           = -124,  /* Data under run error */
    uippStsPaddingErr            = -123,  /* Detected padding error shows the possible data corruption */
    uippStsCFBSizeErr            = -122,  /* Wrong value for crypto CFB block size */
    uippStsPaddingSchemeErr      = -121,  /* Invalid padding scheme  */
    uippStsInvalidCryptoKeyErr   = -120,  /* A compromised key causes suspansion of requested cryptographic operation  */
    uippStsLengthErr             = -119,  /* Wrong value of string length */
    uippStsBadModulusErr         = -118,  /* Bad modulus caused a module inversion failure */
    uippStsLPCCalcErr            = -117,  /* Linear prediction could not be evaluated */
    uippStsRCCalcErr             = -116,  /* Reflection coefficients could not be computed */
    uippStsIncorrectLSPErr       = -115,  /* Incorrect Linear Spectral Pair values */
    uippStsNoRootFoundErr        = -114,  /* No roots are found for equation */
    uippStsJPEG2KBadPassNumber   = -113,  /* Pass number exceeds allowed limits [0,nOfPasses-1] */
    uippStsJPEG2KDamagedCodeBlock= -112,  /* Codeblock for decoding is damaged */
    uippStsH263CBPYCodeErr       = -111,  /* Illegal Huffman code during CBPY stream processing */
    uippStsH263MCBPCInterCodeErr = -110,  /* Illegal Huffman code during MCBPC Inter stream processing */
    uippStsH263MCBPCIntraCodeErr = -109,  /* Illegal Huffman code during MCBPC Intra stream processing */
    uippStsNotEvenStepErr        = -108,  /* Step value is not pixel multiple */
    uippStsHistoNofLevelsErr     = -107,  /* Number of levels for histogram is less than 2 */
    uippStsLUTNofLevelsErr       = -106,  /* Number of levels for LUT is less than 2 */
    uippStsMP4BitOffsetErr       = -105,  /* Incorrect bit offset value */
    uippStsMP4QPErr              = -104,  /* Incorrect quantization parameter */
    uippStsMP4BlockIdxErr        = -103,  /* Incorrect block index */
    uippStsMP4BlockTypeErr       = -102,  /* Incorrect block type */
    uippStsMP4MVCodeErr          = -101,  /* Illegal Huffman code during MV stream processing */
    uippStsMP4VLCCodeErr         = -100,  /* Illegal Huffman code during VLC stream processing */
    uippStsMP4DCCodeErr          = -99,   /* Illegal code during DC stream processing */
    uippStsMP4FcodeErr           = -98,   /* Incorrect fcode value */
    uippStsMP4AlignErr           = -97,   /* Incorrect buffer alignment            */
    uippStsMP4TempDiffErr        = -96,   /* Incorrect temporal difference         */
    uippStsMP4BlockSizeErr       = -95,   /* Incorrect size of block or macroblock */
    uippStsMP4ZeroBABErr         = -94,   /* All BAB values are zero             */
    uippStsMP4PredDirErr         = -93,   /* Incorrect prediction direction        */
    uippStsMP4BitsPerPixelErr    = -92,   /* Incorrect number of bits per pixel    */
    uippStsMP4VideoCompModeErr   = -91,   /* Incorrect video component mode        */
    uippStsMP4LinearModeErr      = -90,   /* Incorrect DC linear mode */
    uippStsH263PredModeErr       = -83,   /* Prediction Mode value error                                       */
    uippStsH263BlockStepErr      = -82,   /* Step value is less than 8                                         */
    uippStsH263MBStepErr         = -81,   /* Step value is less than 16                                        */
    uippStsH263FrameWidthErr     = -80,   /* Frame width is less then 8                                        */
    uippStsH263FrameHeightErr    = -79,   /* Frame height is less than or equal to zero                        */
    uippStsH263ExpandPelsErr     = -78,   /* Expand pixels number is less than 8                               */
    uippStsH263PlaneStepErr      = -77,   /* Step value is less than the plane width                           */
    uippStsH263QuantErr          = -76,   /* Quantizer value is less than or equal to zero, or greater than 31 */
    uippStsH263MVCodeErr         = -75,   /* Illegal Huffman code during MV stream processing                  */
    uippStsH263VLCCodeErr        = -74,   /* Illegal Huffman code during VLC stream processing                 */
    uippStsH263DCCodeErr         = -73,   /* Illegal code during DC stream processing                          */
    uippStsH263ZigzagLenErr      = -72,   /* Zigzag compact length is more than 64                             */
    uippStsFBankFreqErr          = -71,   /* Incorrect value of the filter bank frequency parameter */
    uippStsFBankFlagErr          = -70,   /* Incorrect value of the filter bank parameter           */
    uippStsFBankErr              = -69,   /* Filter bank is not correctly initialized"              */
    uippStsNegOccErr             = -67,   /* Negative occupation count                      */
    uippStsCdbkFlagErr           = -66,   /* Incorrect value of the codebook flag parameter */
    uippStsSVDCnvgErr            = -65,   /* No convergence of SVD algorithm"               */
    uippStsJPEGHuffTableErr      = -64,   /* JPEG Huffman table is destroyed        */
    uippStsJPEGDCTRangeErr       = -63,   /* JPEG DCT coefficient is out of the range */
    uippStsJPEGOutOfBufErr       = -62,   /* Attempt to access out of the buffer    */
    uippStsDrawTextErr           = -61,   /* System error in the draw text operation */
    uippStsChannelOrderErr       = -60,   /* Wrong order of the destination channels */
    uippStsZeroMaskValuesErr     = -59,   /* All values of the mask are zero */
    uippStsQuadErr               = -58,   /* The quadrangle is nonconvex or degenerates into triangle, line or point */
    uippStsRectErr               = -57,   /* Size of the rectangle region is less than or equal to 1 */
    uippStsCoeffErr              = -56,   /* Unallowable values of the transformation coefficients   */
    uippStsNoiseValErr           = -55,   /* Bad value of noise amplitude for dithering"             */
    uippStsDitherLevelsErr       = -54,   /* Number of dithering levels is out of range"             */
    uippStsNumChannelsErr        = -53,   /* Bad or unsupported number of channels                   */
    uippStsCOIErr                = -52,   /* COI is out of range */
    uippStsDivisorErr            = -51,   /* Divisor is equal to zero, function is aborted */
    uippStsAlphaTypeErr          = -50,   /* Illegal type of image compositing operation                           */
    uippStsGammaRangeErr         = -49,   /* Gamma range bounds is less than or equal to zero                      */
    uippStsGrayCoefSumErr        = -48,   /* Sum of the conversion coefficients must be less than or equal to 1    */
    uippStsChannelErr            = -47,   /* Illegal channel number                                                */
    uippStsToneMagnErr           = -46,   /* Tone magnitude is less than or equal to zero                          */
    uippStsToneFreqErr           = -45,   /* Tone frequency is negative, or greater than or equal to 0.5           */
    uippStsTonePhaseErr          = -44,   /* Tone phase is negative, or greater than or equal to 2*PI              */
    uippStsTrnglMagnErr          = -43,   /* Triangle magnitude is less than or equal to zero                      */
    uippStsTrnglFreqErr          = -42,   /* Triangle frequency is negative, or greater than or equal to 0.5       */
    uippStsTrnglPhaseErr         = -41,   /* Triangle phase is negative, or greater than or equal to 2*PI          */
    uippStsTrnglAsymErr          = -40,   /* Triangle asymmetry is less than -PI, or greater than or equal to PI   */
    uippStsHugeWinErr            = -39,   /* Kaiser window is too huge                                             */
    uippStsJaehneErr             = -38,   /* Magnitude value is negative                                           */
    uippStsStrideErr             = -37,   /* Stride value is less than the row length */
    uippStsEpsValErr             = -36,   /* Negative epsilon value error"            */
    uippStsWtOffsetErr           = -35,   /* Invalid offset value of wavelet filter                                       */
    uippStsAnchorErr             = -34,   /* Anchor point is outside the mask                                             */
    uippStsMaskSizeErr           = -33,   /* Invalid mask size                                                           */
    uippStsShiftErr              = -32,   /* Shift value is less than zero                                                */
    uippStsSampleFactorErr       = -31,   /* Sampling factor is less than or equal to zero                                */
    uippStsSamplePhaseErr        = -30,   /* Phase value is out of range: 0 <= phase < factor                             */
    uippStsFIRMRFactorErr        = -29,   /* MR FIR sampling factor is less than or equal to zero                         */
    uippStsFIRMRPhaseErr         = -28,   /* MR FIR sampling phase is negative, or greater than or equal to the sampling factor */
    uippStsRelFreqErr            = -27,   /* Relative frequency value is out of range                                     */
    uippStsFIRLenErr             = -26,   /* Length of a FIR filter is less than or equal to zero                         */
    uippStsIIROrderErr           = -25,   /* Order of an IIR filter is less than or equal to zero                         */
    uippStsDlyLineIndexErr       = -24,   /* Invalid value of the delay line sample index */
    uippStsResizeFactorErr       = -23,   /* Resize factor(s) is less than or equal to zero */
    uippStsInterpolationErr      = -22,   /* Invalid interpolation mode */
    uippStsMirrorFlipErr         = -21,   /* Invalid flip mode                                         */
    uippStsMoment00ZeroErr       = -20,   /* Moment value M(0,0) is too small to continue calculations */
    uippStsThreshNegLevelErr     = -19,   /* Negative value of the level in the threshold operation    */
    uippStsThresholdErr          = -18,   /* Invalid threshold bounds */
    uippStsContextMatchErr       = -17,   /* Context parameter doesn't match the operation */
    uippStsFftFlagErr            = -16,   /* Invalid value of the FFT flag parameter */
    uippStsFftOrderErr           = -15,   /* Invalid value of the FFT order parameter */
    uippStsStepErr               = -14,   /* Step value is not valid */
    uippStsScaleRangeErr         = -13,   /* Scale bounds are out of the range */
    uippStsDataTypeErr           = -12,   /* Bad or unsupported data type */
    uippStsOutOfRangeErr         = -11,   /* Argument is out of range or point is outside the image */
    uippStsDivByZeroErr          = -10,   /* An attempt to divide by zero */
    uippStsMemAllocErr           = -9,    /* Not enough memory allocated for the operation */
    uippStsNullPtrErr            = -8,    /* Null pointer error */
    uippStsRangeErr              = -7,    /* Bad values of bounds: the lower bound is greater than the upper bound */
    uippStsSizeErr               = -6,    /* Wrong value of data size */
    uippStsBadArgErr             = -5,    /* Function arg/param is bad */
    uippStsNoMemErr              = -4,    /* Not enough memory for the operation */
    uippStsSAReservedErr3        = -3,    /*  */
    uippStsErr                   = -2,    /* Unknown/unspecified error */
    uippStsSAReservedErr1        = -1,    /*  */
                                         /*  */
     /* no errors */                     /*  */
    uippStsNoErr                 =   0,   /* No error, it's OK */
                                         /*  */
     /* warnings */                      /*  */
    uippStsNoOperation       =   1,       /* No operation has been executed */
    uippStsMisalignedBuf     =   2,       /* Misaligned pointer in operation in which it must be aligned */
    uippStsSqrtNegArg        =   3,       /* Negative value(s) of the argument in the function Sqrt */
    uippStsInvZero           =   4,       /* INF result. Zero value was met by InvThresh with zero level */
    uippStsEvenMedianMaskSize=   5,       /* Even size of the Median Filter mask was replaced by the odd one */
    uippStsDivByZero         =   6,       /* Zero value(s) of the divisor in the function Div */
    uippStsLnZeroArg         =   7,       /* Zero value(s) of the argument in the function Ln     */
    uippStsLnNegArg          =   8,       /* Negative value(s) of the argument in the function Ln */
    uippStsNanArg            =   9,       /* Not a Number argument value warning                  */
    uippStsJPEGMarker        =   10,      /* JPEG marker was met in the bitstream                 */
    uippStsResFloor          =   11,      /* All result values are floored                        */
    uippStsOverflow          =   12,      /* Overflow occurred in the operation                   */
    uippStsLSFLow            =   13,      /* Quantized LP syntethis filter stability check is applied at the low boundary of [0,pi] */
    uippStsLSFHigh           =   14,      /* Quantized LP syntethis filter stability check is applied at the high boundary of [0,pi] */
    uippStsLSFLowAndHigh     =   15,      /* Quantized LP syntethis filter stability check is applied at both boundaries of [0,pi] */
    uippStsZeroOcc           =   16,      /* Zero occupation count */
    uippStsUnderflow         =   17,      /* Underflow occurred in the operation */
    uippStsSingularity       =   18,      /* Singularity occurred in the operation                                       */
    uippStsDomain            =   19,      /* Argument is out of the function domain                                      */
    uippStsNonIntelCpu       =   20,      /* The target cpu is not Genuine Intel                                         */
    uippStsCpuMismatch       =   21,      /* The library for given cpu cannot be set                                     */
    uippStsNoIppFunctionFound =  22,      /* Application does not contain IPP functions calls                            */
    uippStsDllNotFoundBestUsed = 23,      /* The newest version of IPP dll's not found by dispatcher                     */
    uippStsNoOperationInDll  =   24,      /* The function does nothing in the dynamic version of the library             */
    uippStsInsufficientEntropy=  25,      /* Insufficient entropy in the random seed and stimulus bit string caused the prime/key generation to fail */
    uippStsOvermuchStrings   =   26,      /* Number of destination strings is more than expected                         */
    uippStsOverlongString    =   27,      /* Length of one of the destination strings is more than expected              */
    uippStsAffineQuadChanged =   28,      /* 4th vertex of destination quad is not equal to customer's one               */
    uippStsWrongIntersectROI =   29,      /* Wrong ROI that has no intersection with the source or destination ROI. No operation */
    uippStsWrongIntersectQuad =  30,      /* Wrong quadrangle that has no intersection with the source or destination ROI. No operation */
    uippStsSmallerCodebook   =   31,      /* Size of created codebook is less than cdbkSize argument */
    uippStsSrcSizeLessExpected = 32,      /* DC: The size of source buffer is less than expected one */
    uippStsDstSizeLessExpected = 33,      /* DC: The size of destination buffer is less than expected one */
    uippStsStreamEnd           = 34,      /* DC: The end of stream processed */
    uippStsDoubleSize        =   35,      /* Sizes of image are not multiples of 2 */
    uippStsNotSupportedCpu   =   36,      /* The cpu is not supported */
    uippStsUnknownCacheSize  =   37,      /* The cpu is supported, but the size of the cache is unknown */
    uippStsSymKernelExpected =   38       /* The Kernel is not symmetric*/

} UIppStatus;



typedef enum {uplDeinterlaceImageNone = 0, uplDeinterlaceImageBob = 1} UplDeinterlaceImageMethod;


// deprecated

typedef enum {uplDeinterlaceFrameNone = 0, uplDeinterlaceFrameBob = 1} UplDeinterlaceFrameMethod;

typedef enum {uippDeInterlaceNone, uippDeInterlaceBob = 1} UippDeInterlaceMethod;

typedef struct _BgrHsbParams8w
{
		WORD b[8];
		WORD g[8];
		WORD r[8];
		WORD H[8];
		WORD S[8];
		WORD B[8];
		WORD nMin[8];
		WORD nMid[8];
		WORD nMax[8];
		WORD nRange[8];
		WORD nOffset[8];
		WORD nRange_div_2[8];
		WORD const_x1[8];
		WORD const_x2[8];
		WORD mask[8];
		WORD index[8];
		WORD color[8];
		WORD tmp1[8];
		WORD tmp2[8];

} BgrHsbParams8w;

#ifdef __cplusplus
}
#endif

#endif /* __UPLDEFS_H__ */
