// Reference: Austin Appleby's SMHasher MurmurHash3.cpp (public domain), x86_32 and x64_128.
#include <stdint.h>
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
static inline uint32_t rotl32(uint32_t x, int8_t r){return (x<<r)|(x>>(32-r));}
static inline uint64_t rotl64(uint64_t x, int8_t r){return (x<<r)|(x>>(64-r));}
static inline uint32_t fmix32(uint32_t h){h^=h>>16;h*=0x85ebca6b;h^=h>>13;h*=0xc2b2ae35;h^=h>>16;return h;}
static inline uint64_t fmix64(uint64_t k){k^=k>>33;k*=0xff51afd7ed558ccdULL;k^=k>>33;k*=0xc4ceb9fe1a85ec53ULL;k^=k>>33;return k;}
void MurmurHash3_x86_32(const void*key,int len,uint32_t seed,void*out){
 const uint8_t*data=(const uint8_t*)key;const int nblocks=len/4;uint32_t h1=seed;
 const uint32_t c1=0xcc9e2d51,c2=0x1b873593;
 const uint32_t*blocks=(const uint32_t*)(data+nblocks*4);
 for(int i=-nblocks;i;i++){uint32_t k1;memcpy(&k1,blocks+i,4);k1*=c1;k1=rotl32(k1,15);k1*=c2;h1^=k1;h1=rotl32(h1,13);h1=h1*5+0xe6546b64;}
 const uint8_t*tail=data+nblocks*4;uint32_t k1=0;
 switch(len&3){case 3:k1^=tail[2]<<16;case 2:k1^=tail[1]<<8;case 1:k1^=tail[0];k1*=c1;k1=rotl32(k1,15);k1*=c2;h1^=k1;}
 h1^=len;h1=fmix32(h1);*(uint32_t*)out=h1;}
void MurmurHash3_x64_128(const void*key,const int len,const uint32_t seed,void*out){
 const uint8_t*data=(const uint8_t*)key;const int nblocks=len/16;uint64_t h1=seed,h2=seed;
 const uint64_t c1=0x87c37b91114253d5ULL,c2=0x4cf5ad432745937fULL;
 for(int i=0;i<nblocks;i++){uint64_t k1,k2;memcpy(&k1,data+i*16,8);memcpy(&k2,data+i*16+8,8);
  k1*=c1;k1=rotl64(k1,31);k1*=c2;h1^=k1;h1=rotl64(h1,27);h1+=h2;h1=h1*5+0x52dce729;
  k2*=c2;k2=rotl64(k2,33);k2*=c1;h2^=k2;h2=rotl64(h2,31);h2+=h1;h2=h2*5+0x38495ab5;}
 const uint8_t*tail=data+nblocks*16;uint64_t k1=0,k2=0;
 switch(len&15){
 case 15:k2^=((uint64_t)tail[14])<<48;case 14:k2^=((uint64_t)tail[13])<<40;case 13:k2^=((uint64_t)tail[12])<<32;
 case 12:k2^=((uint64_t)tail[11])<<24;case 11:k2^=((uint64_t)tail[10])<<16;case 10:k2^=((uint64_t)tail[9])<<8;
 case 9:k2^=((uint64_t)tail[8]);k2*=c2;k2=rotl64(k2,33);k2*=c1;h2^=k2;
 case 8:k1^=((uint64_t)tail[7])<<56;case 7:k1^=((uint64_t)tail[6])<<48;case 6:k1^=((uint64_t)tail[5])<<40;
 case 5:k1^=((uint64_t)tail[4])<<32;case 4:k1^=((uint64_t)tail[3])<<24;case 3:k1^=((uint64_t)tail[2])<<16;
 case 2:k1^=((uint64_t)tail[1])<<8;case 1:k1^=((uint64_t)tail[0]);k1*=c1;k1=rotl64(k1,31);k1*=c2;h1^=k1;}
 h1^=len;h2^=len;h1+=h2;h2+=h1;h1=fmix64(h1);h2=fmix64(h2);h1+=h2;h2+=h1;
 ((uint64_t*)out)[0]=h1;((uint64_t*)out)[1]=h2;}
// Generate vectors: for len 0..N, data[i]=(uint8)(i*31+7), several seeds. Output: algo len seed hex
int main(int argc,char**argv){
 int maxlen=argc>1?atoi(argv[1]):300; uint8_t buf[4096]; for(int i=0;i<4096;i++)buf[i]=(uint8_t)(i*31+7);
 uint32_t seeds[]={0,1,42,0xdeadbeef,0xffffffff};
 for(int s=0;s<5;s++)for(int l=0;l<=maxlen;l++){uint32_t h;MurmurHash3_x86_32(buf,l,seeds[s],&h);uint64_t o[2];MurmurHash3_x64_128(buf,l,seeds[s],o);
  printf("%d %u %u %016llx%016llx\n",l,seeds[s],h,(unsigned long long)o[0],(unsigned long long)o[1]);}
 // also the test strings
 const char* strs[]={"key","Hello World!","a$6ajXViSAfFw5pR2kkz3Q28YGrDx$jeaLJ5HFPe","!zhgt#HVY#tV%kPPZ$LXYEo@EqyKjqRJPzUb3*hhASWpdyZAF3!t$V96j9Eb9ivzMH2w4jvuyHaXRxd&YbHz*W8yZGJ#CXjXfqMzNGgf@YMfh*RdZpRXtPQ3mV$N9N!%"};
 uint32_t ss[]={293,420,69,23485};
 for(int i=0;i<4;i++){uint32_t h;MurmurHash3_x86_32(strs[i],strlen(strs[i]),ss[i],&h);fprintf(stderr,"STR %s seed=%u len=%zu x86_32=%u\n",strs[i],ss[i],strlen(strs[i]),h);}
 return 0;}
