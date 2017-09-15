#ifndef __INTERFACE_MOTION_TRACKING_H__
#define __INTERFACE_MOTION_TRACKING_H__



struct MotionTracker
{
	virtual void	SetFeaturePos(float x, float y) = 0;
	virtual void	SetName(TCHAR* pszName) = 0;
	virtual void	GetCurPos(float& x, float& y) = 0;
	
	virtual void	SetDuration(int nIn, int nOut)=0;
	virtual void	SetFeatureDims(float w, float h, bool bIsPNT=true) = 0;
	virtual void	GetCurDims(float& w, float& h) = 0;
	virtual float	GetCurConfidence()= 0;
	

	virtual HRESULT CubicPath(int keyOffset) = 0;
	virtual void	QuadraticBeizer(int iKeyIn, int iKeyOut, float& cx, float& cy)=0;

	virtual HRESULT GetPathLength(int& nLength) = 0;
	virtual HRESULT GetPathFreq(float& framerate) = 0;
	virtual HRESULT GetPathValue(int keyno, float& px, float& py, float& v) = 0;
	virtual HRESULT GetPathOffset(int keyno, float& px, float& py) = 0;
	virtual HRESULT GetPathDims(int keyno, float& pw, float& ph) = 0;

	virtual TCHAR*  GetName() = 0;
	virtual int		GetPathKeyIn() = 0;
	virtual int		GetPathKeyOut() = 0;

	virtual void	ChangePathKeyOut(int nOut) = 0;

	virtual TCHAR*	GetPathGUID() = 0;

};


struct ITrackingManager
{
	virtual MotionTracker*  AddTracker(int nTrackerIdx, TCHAR *szPathGUID=NULL) = 0;
	virtual void			RemoveTracker(TCHAR* pName) = 0;
	virtual MotionTracker*  GetTracker(int nTrackerIdx) = 0;
	virtual int				GetTrackerCount() = 0;

	virtual int			    InitTrackPyramid(int nWidth, int nHeight, DWORD dwForamt = 0) = 0;
	virtual int				UpdateTrackPyramid(BYTE* pBuffer, int nPitch, DWORD dwFormat = 0) = 0;
	virtual void			ReleaseTrackPyramid() = 0;

	virtual void			StartTracker(MotionTracker* p, int frameno) = 0;
	virtual BOOL			GoTracker(MotionTracker* p,int frameno) = 0;
	virtual void			StopTracker(MotionTracker* p, int frameno) = 0;

	
};

struct TrackPoint
{
    float x;
    float y;
};

extern "C" __declspec(dllexport) ITrackingManager* GetTrackingManager(); 

extern "C" __declspec(dllexport) void InitTrackingManager(int nWidth, int nHeight, int nPitch);

extern "C" __declspec(dllexport) void StartTracker(BYTE *pBuffer, TrackPoint pts[], int size);

extern "C" __declspec(dllexport) void GoTracker(BYTE *pBuffer, TrackPoint pts[], int &size);

#endif