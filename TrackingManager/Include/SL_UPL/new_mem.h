#ifndef _NEW_MEM_H_
#define _NEW_MEM_H_

#if !defined(NEW_MEM_SKIP_PRAGMA_LIB)
#ifndef _WIN64
#if _MSC_VER < 1700		// after VS2012, don't use pragma to add additional lib or it'll increase dependency within the solutions.
	#pragma comment(lib, "new_mem_ops_lib.lib")
#endif
#endif
#endif

/* contact patrick.j.fay@intel.com if you have questions.
 * Header for new memory operations. Replacements for memset,memcmp,memcpy,memmove.
 * The 
 */

#ifndef _WIN64
#include "new_mem_ops.h"
#else
#define _new_memcpy memcpy
#endif

#endif

