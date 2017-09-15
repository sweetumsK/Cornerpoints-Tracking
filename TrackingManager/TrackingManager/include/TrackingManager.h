#ifndef __TRACKING_MANAGER_H__
#define __TRACKING_MANAGER_H__
#include "interface_tracking.h"
class CTrackingObject;
class CTrackPath;

class CMotionTracker: public MotionTracker
{
public:
	CMotionTracker(TCHAR *szPathGUID = NULL);
	~CMotionTracker();
	
	virtual void	SetFeaturePos(float x, float y);
	virtual void	SetName(TCHAR* pszName);
	virtual void	GetCurPos(float& x, float& y);

	virtual void	SetDuration(int nIn, int nOut);
	virtual void    SetFeatureDims(float w, float h, bool bIsPNT=true);
	virtual void	GetCurDims(float& w, float& h);
	virtual float	GetCurConfidence();


	virtual HRESULT CubicPath(int keyOffset);
	virtual void	QuadraticBeizer(int iKeyIn, int iKeyOut, float& cx, float& cy);

	virtual HRESULT GetPathLength(int& nLength);
	virtual HRESULT GetPathFreq(float& framerate);
	virtual HRESULT GetPathValue(int keyno, float& px, float& py, float& v);
	virtual HRESULT GetPathDims(int keyno, float& pw, float& ph);
	virtual HRESULT GetPathOffset(int keyno, float& px, float& py);
	virtual TCHAR*  GetName();
	virtual int		GetPathKeyIn();
	virtual int		GetPathKeyOut();

	void	ChangePathKeyOut(int nOut);

	TCHAR*	GetPathGUID() {return m_szPathGUID;};

public:

	CTrackingObject* m_pObjTracker;
	CTrackingPath*   m_pObjPath;

	TCHAR			 m_szName[MAX_PATH];	
	float			 m_fStartPoint[2];
	float			 m_fStartDims[2];

	TCHAR			 m_szPathGUID[38+1];

};

class CTrackingManager: public ITrackingManager
{
public: 
	CTrackingManager(int nNumOfTrackers);
	~CTrackingManager();

	virtual MotionTracker*  AddTracker(int nTrackerIdx, TCHAR *szPathGUID=NULL);
	virtual void			RemoveTracker(TCHAR* pName);
	virtual MotionTracker*  GetTracker(int nTrackerIdx);
	virtual int				GetTrackerCount();

	virtual int			    InitTrackPyramid(int nWidth, int nHeight, DWORD dwForamt);
	virtual int				UpdateTrackPyramid(BYTE* pBuffer, int nPitch, DWORD dwFormat);
	virtual void			ReleaseTrackPyramid();

	virtual void			StartTracker(MotionTracker* p, int frameno);
	virtual BOOL			GoTracker(MotionTracker* p,int frameno);
	virtual void			StopTracker(MotionTracker* p, int frameno);

protected:
	std::vector<MotionTracker*>  m_vecTracker;	
	COptFlowPyrLK*			m_pOptFlow;	
	TrackPyramid*			m_pPyrBuf[2];

};


#endif __TRACKING_MANAGER_H__
