#include "stdafx.h"
#include <windows.h>
#include <tchar.h>

#include <ippi.h>
#include <ippcc.h>
#include "ObjTrackCore.h"
#include "TrackingManager.h"


CTrackingManager::CTrackingManager(int nNumOfTrackers)
{
	m_pOptFlow = NULL;
	m_pPyrBuf[0] = NULL;
	m_pPyrBuf[1] = NULL;

	if (nNumOfTrackers)
	{
		MotionTracker *pMotionTracker = NULL;
		m_vecTracker.resize(nNumOfTrackers, pMotionTracker);
	}

}

CTrackingManager::~CTrackingManager()
{
	if(m_pOptFlow)
	{
		ReleaseTrackPyramid();

		delete m_pOptFlow;
		m_pOptFlow = NULL;
	}

	int size = (int)m_vecTracker.size();
	if(size > 0)
	{
		for(int i = 0; i < size; i++){
			if (m_vecTracker[i] != NULL)
				delete m_vecTracker[i];
			m_vecTracker[i] = NULL;
		}
	}
	m_vecTracker.clear();
}

MotionTracker* CTrackingManager::AddTracker(int nTrackerIdx, TCHAR *szPathGUID)
{
	MotionTracker* p = new CMotionTracker(szPathGUID);

	if(p != NULL)
	{
		TCHAR name[100];
		_stprintf_s(name, _T("Tracker%03d"), nTrackerIdx);
		p->SetName(name);

		//m_vecTracker[nTrackerIdx] = p;
        m_vecTracker.push_back(p);
	}

	return p;
}

void CTrackingManager::RemoveTracker(TCHAR* pName)
{
	std::vector<MotionTracker*>::iterator Iter;
	for(Iter = m_vecTracker.begin(); Iter != m_vecTracker.end(); Iter++)
	{
		MotionTracker* t = *Iter;
		if( _tcscmp(pName, t->GetName()) == 0){

			delete t;
			m_vecTracker.erase(Iter);
			break;
		}
	}

}

MotionTracker* CTrackingManager::GetTracker(int nTrackerIdx)
{
	int nCount = (int)m_vecTracker.size();
	if(nTrackerIdx < nCount)
		return (m_vecTracker[nTrackerIdx]);
	else
		return NULL;
	
}

int CTrackingManager::GetTrackerCount()
{
	return (int)m_vecTracker.size();
}

int CTrackingManager::InitTrackPyramid(int nWidth, int nHeight, DWORD dwForamt)
{
	IppiSize roiSize;
	roiSize.width = nWidth;
	roiSize.height = nHeight;

	int nLevels;
	if (nWidth > 1024)
		nLevels = 5;
	else if (nWidth > 512)
		nLevels = 4;
	else
		nLevels = 3;

	if(NULL == m_pOptFlow)
	{
		m_pOptFlow = new COptFlowPyrLK;
		m_pOptFlow->Init(roiSize, roiSize.width/64, 6, 0.001f);

		m_pPyrBuf[0] = m_pOptFlow->AllocImagePyr(roiSize, nLevels);
		m_pPyrBuf[1] = m_pOptFlow->AllocImagePyr(roiSize, nLevels);


	}
	
	int sx,sy;
	m_pOptFlow->GetImageSize(sx,sy);
	if(sx != nWidth || sy != nHeight)
	{
		ReleaseTrackPyramid();

		m_pOptFlow->Init(roiSize, roiSize.width/64, 6, 0.001f);

		m_pPyrBuf[0] = m_pOptFlow->AllocImagePyr(roiSize, nLevels);
		m_pPyrBuf[1] = m_pOptFlow->AllocImagePyr(roiSize, nLevels);


	}

	return 1;
}

void CTrackingManager::StartTracker(MotionTracker* p, int frameno)
{
	CMotionTracker* pTracker = (CMotionTracker*)p;
	pTracker->m_pObjTracker->SetFeaturePoint(pTracker->m_fStartPoint[0], pTracker->m_fStartPoint[1], m_pPyrBuf[1]);

	TrackPoint32f pt;
	pTracker->m_pObjTracker->GetCenterPos(&pt);
	pt.v = 1.0f;
	pTracker->m_pObjPath->SetPathPos(frameno,&pt);

}

BOOL CTrackingManager::GoTracker(MotionTracker* p, int frameno)
{
	CMotionTracker* pTracker = (CMotionTracker*)p;
	BOOL ret = pTracker->m_pObjTracker->Tracking(m_pPyrBuf[0], m_pPyrBuf[1], m_pOptFlow, frameno);

	TrackPoint32f pt;
	pTracker->m_pObjTracker->GetCenterPos(&pt);

	pTracker->m_pObjPath->SetPathPos(frameno,&pt);
	
    return ret;
}

void CTrackingManager::StopTracker(MotionTracker* p, int frameno)
{
	return ;
}

int	CTrackingManager::UpdateTrackPyramid(BYTE* pBuffer,int nPitch, DWORD dwFormat)
{
	if (m_pPyrBuf[1])
	{
		TrackPyramid* p = m_pPyrBuf[0];
		m_pPyrBuf[0] = m_pPyrBuf[1];
		m_pPyrBuf[1] = p;

		m_pOptFlow->UpdateImagePyrRGB2Gray(pBuffer, nPitch, p);
		return 1;
	}
	else
	{
		return 0;
	}

}

void CTrackingManager::ReleaseTrackPyramid()
{
	if(m_pPyrBuf[0]){
		m_pOptFlow->FreeImagePyr(m_pPyrBuf[0]);
		m_pPyrBuf[0] = 0;
	}

	if(m_pPyrBuf[1]){
		m_pOptFlow->FreeImagePyr(m_pPyrBuf[1]);
		m_pPyrBuf[1] = 0;
	}


}


//////////////////////////////////////////////////////////////////////////
// for motion tracker
//////////////////////////////////////////////////////////////////////////

CMotionTracker::CMotionTracker(TCHAR *szPathGUID)
{
	m_pObjPath = new CTrackingPath;
	m_pObjTracker = new CTrackingObject;

	m_fStartDims[0] = 0.02f;
	m_fStartDims[1] = 0.02f;

	m_fStartPoint[0] = 0;
	m_fStartPoint[1] = 0;

	if (szPathGUID)
		memcpy(m_szPathGUID, szPathGUID, (38+1)*sizeof(TCHAR));
	else
		ZeroMemory(m_szPathGUID, sizeof(m_szPathGUID));
}

CMotionTracker::~CMotionTracker()
{
	delete m_pObjPath;
	delete m_pObjTracker;
}

void CMotionTracker::SetFeaturePos(float x, float y)
{
	m_fStartPoint[0] = x;
	m_fStartPoint[1] = y;
}

void CMotionTracker::SetName(TCHAR* pszName)
{
	_tcscpy_s(m_szName, pszName);
}

void CMotionTracker::GetCurPos(float& x, float& y)
{
	TrackPoint32f pt;
	m_pObjTracker->GetCenterPos(&pt);
	x = pt.x;
	y = pt.y;

	
}

void CMotionTracker::SetFeatureDims(float w, float h, bool bIsPNT)
{
	m_fStartDims[0] = w;
	m_fStartDims[1] = h;

	m_pObjTracker->SetFeatureDims(w, h, bIsPNT);
}

void CMotionTracker::SetDuration(int nIn, int nOut)
{
	m_pObjPath->SetPathRange(nIn, nOut);
}

void CMotionTracker::GetCurDims(float& w, float& h)
{
	// TODO reserved for scale parameters
	//m_pObjTracker->GetFeatureDims(w,h);
	w = m_fStartDims[0];
	h = m_fStartDims[1];
}

float CMotionTracker::GetCurConfidence()
{
	TrackPoint32f pt;
	m_pObjTracker->GetCenterPos(&pt);

	float v = pt.v;
	return v;
}


HRESULT CMotionTracker::CubicPath(int keyOffset)
{
	return S_OK;
}

HRESULT CMotionTracker::GetPathLength(int& framenum)
{
	framenum = (int)m_pObjPath->GetCurrentPathLength();

	return S_OK;

}

HRESULT CMotionTracker::GetPathFreq(float& framerate)
{
	//TODO

	return S_OK;

}

HRESULT CMotionTracker::GetPathValue(int keyno, float& x, float& y, float& v)
{
	TrackPoint32f pt;
	m_pObjPath->GetPathPos(keyno,&pt);
	x = pt.x;
	y = pt.y;
	v = pt.v;
	
	return S_OK;
}

HRESULT CMotionTracker::GetPathDims(int keyno, float& w, float& h)
{
	// TODO 
	//m_pObjTracker->GetFeatureDims(w,h);
	GetCurDims(w,h);
	return S_OK;
}

HRESULT CMotionTracker::GetPathOffset(int keyno, float& x, float& y)
{
	TrackPoint32f pt;
	m_pObjPath->GetPathOffset(keyno,&pt);
	x = pt.x;
	y = pt.y;

	return S_OK;
}



TCHAR*  CMotionTracker::GetName()
{
	return m_szName;
}

int CMotionTracker::GetPathKeyIn()
{
	return m_pObjPath->m_nPathIn;
}

int CMotionTracker::GetPathKeyOut()
{
	return m_pObjPath->m_nPathOut;
}

void CMotionTracker::QuadraticBeizer(int iKeyIn, int iKeyOut, float& cx, float& cy)
{
	m_pObjPath->PieceWiseQuadraticBezier(iKeyIn, iKeyOut, cx, cy);
}

void CMotionTracker::ChangePathKeyOut(int nOut)
{
	if (nOut < m_pObjPath->m_nPathIn)
		return;

	m_pObjPath->m_nPathOut = nOut;
}
