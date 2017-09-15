#ifndef __UPLDISKINF_H_INCLUDED
#define __UPLDISKINF_H_INCLUDED

#if !defined(UPLDISKINF_IGNORE_PRAGMA_LIB) && !defined(UPLDISKINF_EXPORTS)
#if _MSC_VER < 1700		// after VS2012, don't use pragma to add additional lib or it'll increase dependency within the solutions.
	#pragma comment(lib, "upldiskinf.lib")
#endif
#endif



#ifdef UPLDISKINF_EXPORTS
#define UPLDISKINF_EXPIMP __declspec(dllexport)
#else
#define UPLDISKINF_EXPIMP __declspec(dllimport)
#endif

#define UPL_DVR10_DISK_SCORE	1

#ifdef __cplusplus
extern "C"
{
#endif


// another name of function

#define uplGetRWScoreForRWDisk		uplj_1


#ifndef UPLDISKINF_EXPORTS

#endif

// functions implemented in uplcpuinf.dll
// This function does not apply read-only disk.
UPLDISKINF_EXPIMP int uplGetRWScoreForRWDisk(LPCTSTR pPath, double* pMbPerSecRead, double* pMbPerSecWrite, double dMbPerSecReadDefaultIfError, double dMbPerSecWriteDefaultIfError, int test_type);

#ifdef __cplusplus
}
#endif


#endif

