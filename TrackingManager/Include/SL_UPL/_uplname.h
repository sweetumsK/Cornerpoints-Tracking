#ifndef __UPLNAME_H
#define __UPLNAME_H

#include "upldefs.h"

		
#define UPLNAME_UPL_HEADER(name, type, arg)	\
			typedef type (__Stdcall* name##FuncType) arg; \
			extern 	UPL_EXPIMP name##FuncType name;

#define UPLNAME_UPL_BODY(name, type, arg)	\
			UPL_EXPIMP name##FuncType name = 0;

#define UPLNAME_UPL_FUNCTYPE(name)	\
			(name##FuncType)

#define UPLNAME_UPLCPU_ORDINAL(name)	\
			name##_ORDINAL

#define UPLNAME_UPLCPU_HEADER(name, type, arg)	\
			type __Stdcall name arg;

#define UPLNAME_UPLCPU_BODY(name, type, arg)	\
			type __Stdcall name arg 

#endif