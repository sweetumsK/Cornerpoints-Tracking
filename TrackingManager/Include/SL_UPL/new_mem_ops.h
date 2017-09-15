
/* contact patrick.j.fay@intel.com if you have questions.
 * Header for new memory operations. Replacements for memset,memcmp,memcpy,memmove.
 * The 
 */
#ifndef _NEW_MEM_OPS_H_

#define _NEW_MEM_OPS_H_


#define MEM_OPS_USE_SSE2 2
#define MEM_OPS_USE_MMX  1
#define MEM_OPS_USE_REG  0
#define MEM_OPS_NOT_INITED -1

#if defined __cplusplus
extern "C" { /* Begin "C" */
/* Intrinsics use C name-mangling.
 */
#endif /* __cplusplus */

#include <memory.h>

size_t _new_strlen(const char *a);
size_t _new_strncmp(const char *a, const char *b, size_t sz);
void * _new_memcpy(void *a, const void *b,size_t cnt);
void * _new_memset(void *a, size_t pattern,size_t cnt);
void * _new_memmove(void *a, const void *b,size_t cnt);
size_t _new_memcmp(const void *a, const void *b,size_t cnt);
int init_mem_ops_method(void); //returns the old memory operation method
int override_mem_ops_method(int new_method); //forces use of new_method, returns old method
int get_mem_ops_method(void); //returns the current memory operation method
int set_largest_cache_size(int cache_size_in_bytes); //returns the previous cache size
int get_largest_cache_size(void); //returns the current largest cache size

extern size_t inline_new_memcpy_part2(void *a, const void *b,size_t cnt);
extern size_t inline_new_memset_part2(void *a, size_t d,size_t cnt);
extern size_t inline_new_memcmp_part2(const void *a, const void *b,size_t cnt);

_inline size_t inline_new_strcpy(char *a, const char *b)
{
#ifdef _WIN64
	assert( !"[x64] _asm in inline_new_strcpy" );
#else
  __asm {
     mov edx,a
     mov eax,b
topper:
     movzx ecx, byte ptr[eax]
     cmp ecx, 0
     je short write1
     mov ch, byte ptr[eax+1]
     cmp ch, 0
     je short write2
     shl ecx, 16
     mov cl, byte ptr[eax+2]
     cmp cl, 0
     je short write3
     mov ecx, dword ptr[eax]
     mov dword ptr[edx], ecx
     add edx,4
     shr ecx, 24
     cmp cl, 0
     je short bye_bye

     movzx ecx, byte ptr[eax+4]
     cmp ecx, 0
     je short write1
     mov ch, byte ptr[eax+5]
     cmp ch, 0
     je short write2
     shl ecx, 16
     mov cl, byte ptr[eax+6]
     cmp cl, 0
     je short write3
     mov ecx, dword ptr[eax+4]
     ;mov dword ptr[edx+4], ecx
     mov dword ptr[edx], ecx
     add eax,8
     add edx,4
     shr ecx, 24
     cmp cl, 0
     je short bye_bye
     jmp short topper
write3:
     mov byte ptr [edx+2], cl
     shr ecx,16
write2:
     mov word ptr [edx], cx
     jmp short bye_bye
write4:
     mov dword ptr [edx], ecx
     jmp short bye_bye
write1:
     mov byte ptr [edx], cl
bye_bye:
   nop
  }
#endif

}

_inline size_t inline_new_strncmp(const char *a, const char *b, size_t sz)
{
#ifdef _WIN64
	assert( !"[x64] _asm in inline_new_strncmp" );
#else
  __asm {
     mov ecx,sz
     mov eax,a
     mov edx,b

     cmp ecx, 4
     jge got4

     cmp ecx, 1
     jne qk_CkFor2

     movzx ecx, byte ptr[edx]
     cmp byte ptr[eax], cl
     jne short qk_got_ne
     jmp short qk_got_eq

qk_CkFor2:

     cmp ecx, 2
     jl short qk_got_eq
     jg short qk_got3

     movzx ecx, byte ptr[edx]
     cmp byte ptr[eax], cl
     jne short qk_got_ne

     cmp ecx,0
     je short qk_got_eq

     movzx ecx, byte ptr[edx+1]
     cmp byte ptr[eax+1], cl
     jne short qk_got_ne
     jmp short qk_got_eq
qk_got3:
     movzx ecx, word ptr[edx]
     cmp byte ptr[eax], cl
     jne short qk_got_ne
     cmp cl,0
     je short qk_got_eq

     ;movzx ecx, byte ptr[edx+1]
     cmp byte ptr[eax+1], ch
     jne short qk_got_ne
     cmp ch,0
     je short qk_got_eq

     movzx ecx, byte ptr[edx+2]
     cmp byte ptr[eax+2], cl
     jne short qk_got_ne
     ;jmp qk_got_eq
qk_got_eq:
     xor eax,eax
     jmp short do_nop
qk_got_ne:
     sbb eax,eax
     sbb eax,-1
     jmp short do_nop
got4:
     push esi
     push edi
     mov esi,eax
     mov edi,edx
topper:
     sub ecx, 4
     mov eax, dword ptr[esi]
     mov edx, dword ptr[edi]
     cmp eax,edx 
     je short ck_for_zeroes

     cmp al, dl
     jne short got_ne
     cmp dl,0
     je short got_eq
     shr eax,8
     shr edx,8

     cmp al, dl
     jne short got_ne
     cmp dl,0
     je short got_eq
     shr eax,8
     shr edx,8

     cmp al, dl
     jne short got_ne
     cmp dl,0
     je short got_eq

     ; now we know it has to be last byte
     ; don't need to shift, can use whole registers
     ;shr eax,8
     ;shr edx,8

     ;cmp al, dl
     ;jne short got_ne
     ;cmp dl,0
     ;je short got_eq

     cmp eax, edx
     jmp short got_ne

     add esi, 4
     add edi, 4
     cmp ecx, 4
     jge short topper
     jmp short end_loop

ck_for_zeroes:

     ; here we know both registers are equal.
     ; just need to see if one byte is zero so we can quit.

     cmp dl,0
     je short got_eq
     cmp dx,0
     je short got_eq
     shr edx,16

     cmp dl,0
     je short got_eq
     cmp eax,0
     je short got_eq

     add esi, 4
     add edi, 4
     cmp ecx, 4
     jge short topper

end_loop:
     mov edx,edi
     mov eax,esi
noloop:
     cmp ecx, 0
     jle short got_eq
     cmp ecx, 3
     jl ck2
got3:
     movzx ecx, word ptr[edx]
     cmp byte ptr[eax], cl
     jne short got_ne
     cmp cl,0
     je short got_eq

     ;movzx ecx, byte ptr[edx+1]
     cmp byte ptr[eax+1], ch
     jne short got_ne
     cmp ch,0
     je short got_eq

     movzx ecx, byte ptr[edx+2]
     cmp byte ptr[eax+2], cl
     jne short got_ne
     jmp got_eq
ck2:
     cmp ecx, 2
     jl got1
got2:
     movzx ecx, byte ptr[edx]
     cmp byte ptr[eax], cl
     jne short got_ne
     cmp ecx,0
     je short got_eq

     movzx ecx, byte ptr[edx+1]
     cmp byte ptr[eax+1], cl
     jne short got_ne
     jmp got_eq

got1:
     movzx ecx, byte ptr[edx]
     cmp byte ptr[eax], cl
     jne short got_ne
got_eq:
     xor eax,eax
     jmp short bye_bye
got_ne:
     sbb eax,eax
     sbb eax,-1
bye_bye:
     pop edi
     pop esi
do_nop:
   nop
  }
#endif

}

/* This is the inline version of strcmp. It does 1 byte compares
 * inline and is much faster than 'repe scans' for all sizes.
 */
_inline size_t inline_new_strcmp(const char *a, const char *b)
{
#ifdef _WIN64
	assert( !"[x64] _asm in inline_new_strcmp" );
#else
  __asm {
     mov eax,a
     mov edx,b
topper:
     movzx ecx, byte ptr[edx]
     cmp byte ptr[eax], cl
     jne short got_ne
     cmp ecx,0
     je short got_eq

     movzx ecx, byte ptr[edx+1]
     cmp byte ptr[eax+1], cl
     jne short got_ne
     cmp ecx,0
     je short got_eq

     movzx ecx, byte ptr[edx+2]
     cmp byte ptr[eax+2], cl
     jne short got_ne
     cmp ecx,0
     je short got_eq

     movzx ecx, byte ptr[edx+3]
     cmp byte ptr[eax+3], cl
     jne short got_ne
     cmp ecx,0
     je short got_eq

     add edx,4
     add eax,4
     ;jne short topper
     jmp short topper
   got_eq:
     xor eax,eax
     jmp short bye_bye
   got_ne:
     sbb eax,eax
     sbb eax,-1
bye_bye:
  nop
  }
#endif

}

/* This is the inline version of strlen. It does 1 byte compares
 * inline and is much faster than 'repe scans' for all sizes.
 */
_inline size_t inline_new_strlen(const char *a)
{
#ifdef _WIN64
	assert( !"[x64] _asm in inline_new_strlen" );
#else
  __asm {
   mov edx,a
   xor eax,eax 
topper:
     cmp byte ptr[edx], 0
     je bye_bye
     cmp byte ptr[edx+1], 0
     je endit_p1
     cmp byte ptr[edx+2], 0
     je endit_p2
     cmp byte ptr[edx+3], 0
     je endit_p3
     add edx,4 
     add eax,4
     jmp short topper
endit_p3:
     add eax,1
endit_p2:
     add eax,1
endit_p1:
     add eax,1
bye_bye:
  nop
  }
#endif

}

/* This is the inline version of memset. It does 1-4 bytes 
 * inline and then calls the inline_new_memset_part2 if the size is > 4 bytes.
 * The inline part also builds the full 4 byte register to be used for init'ing data.
 * The inline routines seem to only work MSC (not proton)
 */
_inline void * inline_new_memset(void *a, size_t b,size_t cnt)
{
#ifdef _WIN64
	assert( !"[x64] _asm in inline_new_memset" );
#else
  __asm {
topper:
  mov ecx,cnt
  mov eax,b
  mov edx,a

  cmp ecx,1
  jl short bye_bye
  cmp ecx,2
  jl short move1
  mov ah,al
  cmp ecx,3
  jl short move2
  je short move3
  mov ecx,eax
  shl ecx,16
  or eax,ecx
  mov ecx,cnt
  cmp ecx,4
  je move4
movegt4:
  call inline_new_memset_part2
  mov edx,a
  jmp short bye_bye
move4:
  mov dword ptr[edx], eax
  jmp short bye_bye
move2:
  mov word ptr[edx], ax
  jmp short bye_bye
move3:
  mov word ptr[edx], ax
  mov byte ptr[edx+2], al
  jmp short bye_bye
move1:
  mov byte ptr[edx], al
bye_bye:
  mov eax,edx
  nop
  }
#endif

}

/* the inline_new_memcmp does 1-3 bytes inline and call inline_new_memcmp_part2 
 * if the size > 3.
 */

_inline size_t inline_new_memcmp(const void *a, const void *b,size_t cnt)
{
#ifdef _WIN64
	assert( !"[x64] _asm in inline_new_memcmp" );
#else
  __asm {
topper:
   mov       eax, a
   mov       edx, b
   mov       ecx, cnt

   cmp ecx,1
   jne short ck2
   movzx ecx, byte ptr[eax]
   movzx eax, byte ptr[edx]
   cmp ecx, eax
   jne short setitq
   jmp short got_zero
ck2:
   cmp ecx,2
   jne short ck3
   movzx ecx, word ptr[eax]
   movzx eax, word ptr[edx]
   cmp cl, al
   jne short setitq
   cmp ecx, eax
   jne short setitq
   jmp short got_zero
ck3:
   jl short got_zero
   ; do a quick check of the 1st byte. This is an optimization.
   ; Frequently, the first byte is not equal...
   movzx ecx, byte ptr[eax]
   cmp cl, byte ptr[edx]
   jne short setitq
   mov ecx,cnt
   call inline_new_memcmp_part2
   jmp bye_bye
setitq:
   sbb eax,eax
   sbb eax,-1
   jmp short bye_bye
got_zero:
   xor eax,eax
bye_bye:
   nop
  }
#endif

}

/* inline_new_memcpy does 1-4 bytes inline. If the size > 4 then
   it calls the part2 routine. The inline version is safe for overlaping
   source and dest. But I think the inline version only works for MSC (not electron).
   All of the inline routines call the part2 routine passing the args in edx,eax,ecx
   (the args are not pushed on the stack).

 */
   

_inline void * inline_new_memcpy(void *a, const void *b,size_t cnt)
{
#ifdef _WIN64
	assert( !"[x64] _asm in inline_new_memcpy" );
#else
  __asm {
   mov ecx,cnt
   mov eax,b
   mov edx,a

   cmp ecx,4
   jg short call_cpy
   cmp ecx,1
   jne short ck2
   movzx ecx, byte ptr [eax]
   mov byte ptr [edx], cl
   mov eax,edx
   jmp short bye_bye
ck2: 
   cmp ecx,2
   jne short ck3
   movzx ecx, word ptr [eax]
   mov word ptr [edx], cx
   mov eax,edx
   jmp short bye_bye
ck3:
   cmp ecx,3
   jl short skip_call
   jne short ck4
   movzx ecx, word ptr [eax]
   ;shl ecx,16
   ;mov cl, byte ptr [eax+2]
   movzx eax, byte ptr [eax+2]
   mov byte ptr [edx+2], al
   ;mov byte ptr [edx+2], cl
   ;shr ecx,16
   mov word ptr [edx], cx
   mov eax,edx
   jmp short bye_bye
ck4:
   ;cmp ecx,4
   ;jne short call_cpy
   mov ecx, dword ptr [eax]
   mov dword ptr [edx], ecx
   mov eax,edx
   jmp short bye_bye
call_cpy:
   ;jl short skip_call
   call inline_new_memcpy_part2
skip_call:
   mov eax,a
bye_bye:
   nop
  }
#endif

}

/* this is a copy of inline_new_memcpy*/
_inline void * inline_new_memmove(void *a, const void *b,size_t cnt)
{
#ifdef _WIN64
	assert( !"[x64] _asm in inline_new_memmove" );
#else
  __asm {
   mov ecx,cnt
   mov eax,b
   mov edx,a

   cmp ecx,1
   jne short ck2
   movzx ecx, byte ptr [eax]
   mov byte ptr [edx], cl
   mov eax,edx
   jmp short bye_bye
ck2: 
   cmp ecx,2
   jne short ck3
   movzx ecx, word ptr [eax]
   mov word ptr [edx], cx
   mov eax,edx
   jmp short bye_bye
ck3:
   cmp ecx,3
   jne short ck4
   movzx ecx, word ptr [eax]
   shl ecx,16
   ;movzx ecx, byte ptr [eax+2]
   mov cl, byte ptr [eax+2]
   mov byte ptr [edx+2], cl
   shr ecx,16
   mov word ptr [edx], cx
   mov eax,edx
   jmp short bye_bye
ck4:
   cmp ecx,4
   jne short call_cpy
   mov ecx, dword ptr [eax]
   mov dword ptr [edx], ecx
   mov eax,edx
   jmp short bye_bye
call_cpy:
   jl short skip_call
   call inline_new_memcpy_part2
skip_call:
   mov eax,a
bye_bye:
   nop
  }
#endif

}



#if defined __cplusplus
};
/* Intrinsics use C name-mangling.
 */
#endif /* __cplusplus */

#endif
