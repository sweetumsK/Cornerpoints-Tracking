#ifndef __OBJ_TRACKING_H__
#define __OBJ_TRACKING_H__

#include <ippcore.h>
#include <ipps.h>
#include <ippcv.h>
#include <vector>

enum _SEARCH_PATTERN_
{
	SEARCH_PATTERN_PNT = 0,
	SEARCH_PATTERN_DIAMOND_3X3,
	SEARCH_PATTERN_SQUARE_3X3,
	SEARCH_PATTERN_LINE_3X1,
	SEARCH_PATTERN_LINE_1X3,
	SEARCH_PATTERN_DIAMOND_5X3,
	SEARCH_PATTERN_DIAMOND_3X5,

};

typedef struct _TRACK_POINT_32F_{
	float x;
	float y;

	float v;

	_TRACK_POINT_32F_(){
		x=0; y=0; v=0;
	}
} TrackPoint32f;

typedef struct{
	Ipp8u* pImage;
	IppiSize size;
	int nPitch;
	int nBytePerPixel;
} TrackImage;

typedef IppiPyramid TrackPyramid;

class COptFlowPyrLK;
//////////////////////////////////////////////////////////////////////////

class CTrackingPath
{
public:
	CTrackingPath();
	~CTrackingPath();
	
	void	SetPathPos(int iKey, TrackPoint32f* p);
	void	SetPathRange(int nInPos, int nOutPos);

	int		CheckPathRange(int iKey);

	int		GetPathPos(int iKey, TrackPoint32f* p);
	int		GetPathOffset(int iKey, TrackPoint32f* p);
	int		GetPathDuration();
	size_t	GetCurrentPathLength();
	
	void	Reset();
	
	void	PieceWiseQuadraticBezier(int iKeyIn, int iKeyOut, float& ctlx, float& ctly);


	int				m_nPathIn;
	int				m_nPathOut;
	int				m_nInitKey;
	std::vector<TrackPoint32f> m_vecPath;


};

class CTrackingObject
{
public:
	CTrackingObject();
	~CTrackingObject();

	// select our tracking obj
	void SetFeaturePoint(float x, float y, TrackPyramid* p);
	void GetStartPoint(float& x, float& y);

	void SetFeatureDims(float w, float h, bool bIsPNT=true);
	void GetFeatureDims(float& w, float& h);

	bool IsPNT() { return m_bIsPNT;};

	// track orth rect
	void SetFeaturePoint(TrackPoint32f corner[4], TrackPyramid* p);
	void GetTrackingPoint(TrackPoint32f corner[4]);
	
	// 
	BOOL Tracking(TrackPyramid* pPrev, TrackPyramid* pCur, COptFlowPyrLK* pKernel, int nFrameNo);
	
	void GetCenterPos(TrackPoint32f * p);
	void GetMotion(TrackPoint32f* p);
	
	void Reset(TrackPyramid* p);


protected:
	void RefineFeature3x3(TrackImage* pImage, TrackPoint32f* p );
	BOOL GetTrackImage(TrackImage* pImage, TrackPyramid* pPyr, int nLevel);

	void Get1x1DiamondPattern(TrackPoint32f seed, TrackPoint32f Group[5]);
	void Get3x3DiamondPattern(TrackPoint32f seed, TrackPoint32f Group[5]);
	void Get3x3SquarePattern(TrackPoint32f seed, TrackPoint32f Group[9]);
	void Get3x1LinePattern(TrackPoint32f seed, TrackPoint32f Group[3]);
	void Get1x3LinePattern(TrackPoint32f seed, TrackPoint32f Group[3]);
	void Get5x3DiamondPattern(TrackPoint32f seed, TrackPoint32f Group[7]);
	void Get3x5DiamondPattern(TrackPoint32f seed, TrackPoint32f Group[7]);

	TrackPoint32f	m_TP[10];
	int				m_nTPNum;

	TrackPoint32f	m_vCenter;
	TrackPoint32f	m_vMotion;

	TrackPoint32f   m_vStart;

	int				m_iImgSize[2];

private:
	float			m_fDimW;
	float			m_fDimH;
	int				m_nSearchPattern; //enum _SEARCH_PATTERN_

	bool			m_bIsPNT;

};


class COptFlowPyrLK
{
public:
	COptFlowPyrLK();
	~COptFlowPyrLK();

	IppiPyramid* CreateImagePyr(Ipp8u * pFrame, int iStep, IppiSize roiSize, int numLevel);
	IppiPyramid* AllocImagePyr(IppiSize roiSize, int numLevel);

	void UpdateImagePyrRGB2Gray(BYTE* pRGB, int nPitch, TrackPyramid* pPyr);
	void UpdateImagePyr(IppiPyramid* pPyr,Ipp8u* pImage,int iStep, IppiSize roiSize);
	void FreeImagePyr(IppiPyramid* p);

	void Init(IppiSize picsize, int winsize,int numIter, float threshold);
	void Run(IppiPyramid * pPyr1, IppiPyramid* pPyr2,IppiPoint_32f* prevPt,IppiPoint_32f* nextPt, int numfeat, 
		Ipp8s *pstatus, Ipp32f * pError, int nFrameNo); // the ipp function included in this function is not thread-safe
	void Close();

	void GetImageSize(int& x, int& y);

	int GetSearchWindowSize() { return m_winSize;};

protected:
	IppiOptFlowPyrLK*	m_pOF;
	int					m_winSize;
	float				m_threshold;
	int					m_numIter;
	IppiSize			m_roiSize;
};



#endif