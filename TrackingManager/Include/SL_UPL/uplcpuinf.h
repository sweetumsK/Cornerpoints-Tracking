#ifndef __UPLCPUINF_H_INCLUDED
#define __UPLCPUINF_H_INCLUDED

#if !defined(UPLCPUINF_IGNORE_PRAGMA_LIB) && !defined(UPLCPUINF_EXPORTS)
#if _MSC_VER < 1700		// after VS2012, don't use pragma to add additional lib or it'll increase dependency within the solutions.
	#pragma comment(lib, "uplcpuinf.lib")
#endif
#endif


#ifdef UPLCPUINF_EXPORTS
#define UPLCPUINF_EXPIMP __declspec(dllexport)
#else
#define UPLCPUINF_EXPIMP __declspec(dllimport)
#endif


#define CPU_VENDOR_UNKNOWN		0
#define CPU_VENDOR_INTEL		1
#define CPU_VENDOR_AMD			2
#define CPU_VENDOR_CYRIX		3
#define CPU_VENDOR_VIA			4
#define CPU_VENDOR_TRANSMETA	5

#define CPU_UNKNOWN_X86			0
#define CPU_INTEL_X86			1
#define CPU_AMD_X86				2
#define CPU_CYRIX_X86			3
#define CPU_VIA_X86				4
#define CPU_TRANSMETA_X86		5


// feature 
#define CPU_IA_MMX_FLAG			0x00800000
#define CPU_IA_SSE_FLAG			0x02000000
#define CPU_IA_SSE2_FLAG		0x04000000
#define CPU_IA_SSE3_FLAG		0x00000001
#define CPU_IA_SSE4_FLAG		0x00000200

#define MAX_CORE_COUNT			64



typedef struct tagDsUplCpuInfo
{
	BOOL  bSupportCpuId;
	DWORD dwUvVendor;			// defined by uplcpuinf
	DWORD dwVendorId[3];			// 3 dword from cpuid command
	char  szVendorId[16];			// convert dwVendorId[3] to string
	char  szBrandString[48+4];
	//
	DWORD dwMaxStdFuncNum;
	DWORD dwMaxExtFuncNum;
	DWORD dwType;
	DWORD dwFamily;
	DWORD dwExtFamily;
	DWORD dwModel;
	DWORD dwExtModel;
	DWORD dwSteppingId;
	DWORD dwDefaultApicId;
	DWORD dwClflushChunkCount;
	DWORD dwBrandId;
	BOOL  bPSN;
	DWORD dwPSN0;
	DWORD dwPSN1;
	DWORD dwProcessorSig;
	DWORD dwFeature;
	DWORD dwFeatureReserved;
	DWORD dwFeatureSoftware;
	DWORD dwFeatureReservedSoftware;
	BYTE  szProcessorName[48];

	DWORD dwAffinityMask;
	int core_cpu_index;

	PVOID p_cpu_score;

	//
	DWORD dwCacheLineSizeL1;
		DWORD dwCacheLineSizeL1_I;
		DWORD dwCacheLineSizeL1_D;

	DWORD dwCacheLineSizeL2;
	DWORD dwCacheLineSizeL3;

	DWORD dwCacheSizeL1;
		DWORD dwCacheSizeL1_I;
		DWORD dwCacheSizeL1_D;
	DWORD dwCacheSizeL2;
	DWORD dwCacheSizeL3;
	//
	BOOL  bFilled;
	DWORD dwReserved[16];
} DsUplCpuInfo;


#ifdef __cplusplus
extern "C"
{
#endif


// another name of function

#define uplGetCpuInfo					upli_1
#define uplGetCpuInfoFeatureSoftware	upli_2
#define uplSetCpuInfoFeatureSoftware	upli_3
#define uplIsIA_MMX_CPU					upli_4
#define uplIsIA_SSE_CPU					upli_5
#define uplIsIA_SSE2_CPU				upli_6
//#define uplGetProcessorScore			upli_7
#define uplGetProcessorCount			upli_8
#define uplSelectCPU					upli_9
#define uplGetCpuInfoN					upli_10
//#define uplGetProcessorScoreN			upli_11
#define uplIsIA_SSE3_CPU				upli_12
#define uplIsIA_SSE4_CPU				upli_13

#ifdef _WIN64
#define uplIsMMXCPU()	TRUE
#define uplIsSSECPU()	TRUE
#define uplIsSSE2CPU()	TRUE
#else
#define uplIsMMXCPU()	(uplIsIA_MMX_CPU())
#define uplIsSSECPU()	(uplIsIA_SSE_CPU())
#define uplIsSSE2CPU()	(uplIsIA_SSE2_CPU())
#endif
#define uplIsSSE3CPU()	(uplIsIA_SSE3_CPU())
#define uplIsSSE4CPU()	(uplIsIA_SSE4_CPU())


#ifndef UPLCPUINF_EXPORTS

#endif

//functions implemented in uplcpuinf.dll
UPLCPUINF_EXPIMP const DsUplCpuInfo* uplGetCpuInfo();
UPLCPUINF_EXPIMP DWORD uplGetCpuInfoFeatureSoftware();
UPLCPUINF_EXPIMP DWORD uplSetCpuInfoFeatureSoftware(DWORD dwFeatureSoftware);
UPLCPUINF_EXPIMP BOOL uplIsIA_MMX_CPU();
UPLCPUINF_EXPIMP BOOL uplIsIA_SSE_CPU();
UPLCPUINF_EXPIMP BOOL uplIsIA_SSE2_CPU();
UPLCPUINF_EXPIMP BOOL uplIsIA_SSE3_CPU();
UPLCPUINF_EXPIMP BOOL uplIsIA_SSE4_CPU();
// processor level 1700(+-50): Pentium 1.7G (AMD XP 1.47G)
// processor level 2000(+-50): Pentium 2.0G (AMD XP 1.67G)
// processor level 2100(+-50): Pentium 2.1G (AMD XP 1.73G)
// ...
UPLCPUINF_EXPIMP int uplGetProcessorScore();
UPLCPUINF_EXPIMP unsigned int uplGetProcessorCount(unsigned int* CoreNum, unsigned int* LogicalNumPerCore);
UPLCPUINF_EXPIMP int uplSelectCPU(int physical_cpu, int logical_cpu_index_on_physical);


UPLCPUINF_EXPIMP const DsUplCpuInfo* uplGetCpuInfoN(int cpu_index);
UPLCPUINF_EXPIMP int uplGetProcessorScoreN(int cpu_index);


#ifdef __cplusplus
}
#endif


#endif

