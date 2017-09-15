#include "stdafx.h"
#include <windows.h>
#include <ipps.h>
#include <ippi.h>
#include <ippcv.h>
#include <math.h>
#include "ObjTrackCore.h"

//#define _DBGLOG_OPTICAL_FLOW_

extern void LogMsg(char* pszFormat, ... );


static inline float fClip3(float low, float high, float x)
{
	x = fmax(x, low);
	x = fmin(x, high);

	return x;
}

static void SobelGradient(Ipp8u* pImage, int iStep, int x, int y, IppiPoint_32f* pGrad);
static float minEigenValue(float gxx, float gxy, float gyy );

CTrackingObject::CTrackingObject()
{
	m_fDimW = m_fDimH = 0.0f;

	m_nSearchPattern = SEARCH_PATTERN_PNT;

	m_bIsPNT = true;

}

CTrackingObject::~CTrackingObject()
{

}

void CTrackingObject::SetFeaturePoint(TrackPoint32f corner[4], TrackPyramid* pCur)
{
	m_nTPNum = 4;
	int nPtNum = m_nTPNum;

	TrackPoint32f* pat = m_TP; 

	TrackImage tImage;

	GetTrackImage(&tImage, pCur, 0);

	float fscale = 1.0f;

	m_iImgSize[0] = tImage.size.width;
	m_iImgSize[1] = tImage.size.height;

	for(int i = 0; i< m_nTPNum; i++)
	{
		pat[i].x = corner[i].x * fscale * tImage.size.width;
		pat[i].y = corner[i].y * fscale * tImage.size.height;
	}

	/*
	for(int level = nStartLevel; level >=0; level --)
	{
		GetTrackImage(&tImage, pCur, level);
		for(int i = 0; i < nPtNum; i++){
			RefineFeature3x3(&tImage, &pat[i]);

			if(level > 0)
			{
				pat[i].x *= 2.0;
				pat[i].y *= 2.0f;
			}
		}
	}
	*/

}

void CTrackingObject::GetTrackingPoint(TrackPoint32f corner[4])
{
	for(int i = 0; i < 4; i++)
	{
		corner[i].x = m_TP[i].x / m_iImgSize[0];
		corner[i].y = m_TP[i].y / m_iImgSize[1];
	}
}
void CTrackingObject::GetStartPoint(float& x, float& y)
{
	x = m_vStart.x;
	y = m_vStart.y;
}

void CTrackingObject::SetFeatureDims(float w, float h, bool bIsPNT)
{
	m_fDimW = w;
	m_fDimH = h;

	m_bIsPNT = bIsPNT;
}

void CTrackingObject::GetFeatureDims(float& w, float& h)
{
	w = m_fDimW;
	h = m_fDimH;
}

void CTrackingObject::SetFeaturePoint(float x, float y, TrackPyramid* pCur)
{
	TrackPoint32f seed;
	TrackPoint32f* pat = m_TP; 

	m_vStart.x = x;
	m_vStart.y = y;

	TrackImage tImage;
	GetTrackImage(&tImage, pCur, 0);
	m_iImgSize[0] = tImage.size.width;
	m_iImgSize[1] = tImage.size.height;

	// Using frame resolution and search rect to decide start Pyramid level
	float fscale = 0.125f;
	int nStartLevel = 3;
	if(tImage.size.width < 1024 || ((float)m_iImgSize[0]*m_fDimW < 50.0 || (float)m_iImgSize[1]*m_fDimH < 50.0))
	{
		fscale = 0.25f;
		nStartLevel = 2;
	}
	if(tImage.size.width < 512 || ((float)m_iImgSize[0]*m_fDimW < 25.0 || (float)m_iImgSize[1]*m_fDimH < 25.0))
	{
		fscale = 0.5f;
		nStartLevel = 1;
	}

	m_vCenter.x = x * tImage.size.width;
	m_vCenter.y = y * tImage.size.height;

	seed.x = x * fscale * tImage.size.width;
	seed.y = y * fscale * tImage.size.height;

	// by start Pyramid level and feature dims to decide which search pattern
	if (m_bIsPNT)
	{
		m_nSearchPattern = SEARCH_PATTERN_PNT;
		m_nTPNum = 5;
	}
	else
	{
		if ((float)m_iImgSize[0]*m_fDimW > 75.0 && (float)m_iImgSize[1]*m_fDimH > 75.0)
		{
			m_nSearchPattern = SEARCH_PATTERN_SQUARE_3X3;
			m_nTPNum = 9;
		}
		else if ((float)m_iImgSize[0]*m_fDimW > 75.0)
		{
			m_nSearchPattern = SEARCH_PATTERN_DIAMOND_5X3;
			m_nTPNum = 7;
		}
		else if ((float)m_iImgSize[1]*m_fDimH > 75.0)
		{
			m_nSearchPattern = SEARCH_PATTERN_DIAMOND_3X5;
			m_nTPNum = 7;
		}
		else if ((float)m_iImgSize[0]*m_fDimW > (float)m_iImgSize[1]*m_fDimH && (float)m_iImgSize[1]*m_fDimH < 25.0)
		{
			m_nSearchPattern = SEARCH_PATTERN_LINE_3X1;
			m_nTPNum = 3;
		}
		else if ((float)m_iImgSize[0]*m_fDimW < (float)m_iImgSize[1]*m_fDimH && (float)m_iImgSize[0]*m_fDimW < 25.0)
		{
			m_nSearchPattern = SEARCH_PATTERN_LINE_1X3;
			m_nTPNum = 3;
		}
		else
		{
			m_nSearchPattern = SEARCH_PATTERN_DIAMOND_3X3;
			m_nTPNum = 5;
		}
	}

	// generate search pattern
	switch(m_nSearchPattern)
	{
	case SEARCH_PATTERN_SQUARE_3X3:
		Get3x3SquarePattern(seed, pat);
		break;
	case SEARCH_PATTERN_DIAMOND_5X3:
		Get5x3DiamondPattern(seed, pat);
		break;
	case SEARCH_PATTERN_DIAMOND_3X5:
		Get3x5DiamondPattern(seed, pat);
		break;
	case SEARCH_PATTERN_LINE_3X1:
		Get3x1LinePattern(seed, pat);
		break;
	case SEARCH_PATTERN_LINE_1X3:
		Get1x3LinePattern(seed, pat);
		break;
	case SEARCH_PATTERN_DIAMOND_3X3:
		Get3x3DiamondPattern(seed, pat);
		break;
	case SEARCH_PATTERN_PNT:
	default:
		Get1x1DiamondPattern(seed, pat);
		break;
	}

	for(int level = nStartLevel; level >=0; level --)
	{
		if (m_nSearchPattern != SEARCH_PATTERN_PNT)
		{
			GetTrackImage(&tImage, pCur, level);
			for(int i = 0; i < m_nTPNum; i++)
			{
				RefineFeature3x3(&tImage, &pat[i]);
				if (level > 0)
				{
					pat[i].x *= 2.0f;
					pat[i].y *= 2.0f;
				}
			}
		}
		else
		{
			if (level > 0)
			{
				for(int i = 0; i < m_nTPNum; i++)
				{
					pat[i].x *= 2.0f;
					pat[i].y *= 2.0f;
				}
			}
		}
	}

	// update refined m_vCenter
	float fRefinedX = 0.0f, fRefinedY = 0.0f;
	for (int i=0; i<m_nTPNum; i++)
	{
		fRefinedX += m_TP[i].x;
		fRefinedY += m_TP[i].y;
	}
	m_vCenter.x = (fRefinedX/(float)m_nTPNum);
	m_vCenter.y = (fRefinedY/(float)m_nTPNum);

}

void CTrackingObject::Reset(TrackPyramid* p)
{
	SetFeaturePoint(m_vStart.x, m_vStart.y, p);
}

void CTrackingObject::Get1x1DiamondPattern(TrackPoint32f seed, TrackPoint32f pat[5])
{
	pat[0].x = seed.x;
	pat[0].y = seed.y;

	pat[1].x = pat[0].x + 1;
	pat[1].y = pat[0].y;

	pat[2].x = pat[0].x - 1;
	pat[2].y = pat[0].y;

	pat[3].x = pat[0].x ;
	pat[3].y = pat[0].y + 1;

	pat[4].x = pat[0].x;
	pat[4].y = pat[0].y - 1;
}

void CTrackingObject::Get3x3DiamondPattern(TrackPoint32f seed, TrackPoint32f pat[5])
{
	pat[0].x = seed.x;
	pat[0].y = seed.y;

	pat[1].x = pat[0].x + 3;
	pat[1].y = pat[0].y;

	pat[2].x = pat[0].x - 3;
	pat[2].y = pat[0].y;

	pat[3].x = pat[0].x ;
	pat[3].y = pat[0].y + 3;

	pat[4].x = pat[0].x;
	pat[4].y = pat[0].y - 3;
}

void CTrackingObject::Get3x3SquarePattern(TrackPoint32f seed, TrackPoint32f pat[9])
{
	pat[0].x = seed.x;
	pat[0].y = seed.y;

	pat[1].x = pat[0].x + 3;
	pat[1].y = pat[0].y;

	pat[2].x = pat[0].x - 3;
	pat[2].y = pat[0].y;

	pat[3].x = pat[0].x ;
	pat[3].y = pat[0].y + 3;

	pat[4].x = pat[0].x;
	pat[4].y = pat[0].y - 3;

	pat[5].x = pat[0].x + 3;
	pat[5].y = pat[0].y + 3;

	pat[6].x = pat[0].x + 3;
	pat[6].y = pat[0].y - 3;

	pat[7].x = pat[0].x - 3;
	pat[7].y = pat[0].y + 3;

	pat[8].x = pat[0].x - 3;
	pat[8].y = pat[0].y - 3;
}

void CTrackingObject::Get3x1LinePattern(TrackPoint32f seed, TrackPoint32f pat[3])
{
	pat[0].x = seed.x;
	pat[0].y = seed.y;

	pat[1].x = pat[0].x + 3;
	pat[1].y = pat[0].y;

	pat[2].x = pat[0].x - 3;
	pat[2].y = pat[0].y;
}

void CTrackingObject::Get1x3LinePattern(TrackPoint32f seed, TrackPoint32f pat[3])
{
	pat[0].x = seed.x;
	pat[0].y = seed.y;

	pat[1].x = pat[0].x;
	pat[1].y = pat[0].y + 3;

	pat[2].x = pat[0].x;
	pat[2].y = pat[0].y - 3;
}

void CTrackingObject::Get5x3DiamondPattern(TrackPoint32f seed, TrackPoint32f pat[7])
{
	pat[0].x = seed.x;
	pat[0].y = seed.y;

	pat[1].x = pat[0].x + 3;
	pat[1].y = pat[0].y;

	pat[2].x = pat[0].x - 3;
	pat[2].y = pat[0].y;

	pat[3].x = pat[0].x ;
	pat[3].y = pat[0].y + 3;

	pat[4].x = pat[0].x;
	pat[4].y = pat[0].y - 3;

	pat[5].x = pat[0].x + 6;
	pat[5].y = pat[0].y;

	pat[6].x = pat[0].x - 6;
	pat[6].y = pat[0].y;
}

void CTrackingObject::Get3x5DiamondPattern(TrackPoint32f seed, TrackPoint32f pat[7])
{
	pat[0].x = seed.x;
	pat[0].y = seed.y;

	pat[1].x = pat[0].x + 3;
	pat[1].y = pat[0].y;

	pat[2].x = pat[0].x - 3;
	pat[2].y = pat[0].y;

	pat[3].x = pat[0].x ;
	pat[3].y = pat[0].y + 3;

	pat[4].x = pat[0].x;
	pat[4].y = pat[0].y - 3;

	pat[5].x = pat[0].x;
	pat[5].y = pat[0].y + 6;

	pat[6].x = pat[0].x;
	pat[6].y = pat[0].y - 6;
}

void CTrackingObject::RefineFeature3x3(TrackImage* pImage, TrackPoint32f* p )
{
	IppiPoint_32f gradxy[25];

	int m,n;

	float gxx, gyy,gxy;
	gxx = gyy = gxy = 0;
	
	int x, y;
	x = (int)(p->x + 0.5f);
	y = (int)(p->y + 0.5f);
	int i = 0;

	for(m = -2; m <=2; m++){
		for(n = -2; n <=2; n++){
			SobelGradient(pImage->pImage, pImage->nPitch, x + n, y + m, &gradxy[i++]);
		}
	}

	int maxx, maxy;
	float maxev = -1e8;
	maxx = 0;
	maxy = 0;
	
	float gx, gy;

	for(m = -1; m <= 1; m ++){
		for(n = -1; n <=1; n++)
		{
			int idx = (m + 2)*5 + (n+2);

			gx = gradxy[idx-6].x; gy = gradxy[idx-6].y;
			gxx = gx * gx; 
			gyy = gy * gy;
			gxy = gx * gy;

			gx = gradxy[idx-5].x; gy = gradxy[idx-5].y;
			gxx += gx * gx; 
			gyy += gy * gy;
			gxy += gx * gy;

			gx = gradxy[idx-4].x; gy = gradxy[idx-4].y;
			gxx += gx * gx; 
			gyy += gy * gy;
			gxy += gx * gy;

			//////////////////////////////////////////////////////////////////////////
			gx = gradxy[idx-1].x; gy = gradxy[idx-1].y;
			gxx += gx * gx; 
			gyy += gy * gy;
			gxy += gx * gy;

			gx = gradxy[idx].x; gy = gradxy[idx].y;
			gxx += gx * gx; 
			gyy += gy * gy;
			gxy += gx * gy;

			gx = gradxy[idx+1].x; gy = gradxy[idx+1].y;
			gxx += gx * gx; 
			gyy += gy * gy;
			gxy += gx * gy;

			//////////////////////////////////////////////////////////////////////////
			gx = gradxy[idx+4].x; gy = gradxy[idx+4].y;
			gxx += gx * gx; 
			gyy += gy * gy;
			gxy += gx * gy;

			gx = gradxy[idx+5].x; gy = gradxy[idx+5].y;
			gxx += gx * gx; 
			gyy += gy * gy;
			gxy += gx * gy;

			gx = gradxy[idx+6].x; gy = gradxy[idx+6].y;
			gxx += gx * gx; 
			gyy += gy * gy;
			gxy += gx * gy;
			
			float ev = minEigenValue(gxx,gxy,gyy);

			// we should let center point will have higher weighting
			if ((m==0 && n==0) && ev >= maxev)
			{
				maxev = ev;
				maxx = n; maxy = m;
			}
			else if (ev > maxev)
			{
				maxev = ev;
				maxx = n; maxy = m;
			}
		}
	}

	p->x = float(x + maxx);
	p->y = float(y + maxy);
	p->v = maxev;

}

void CTrackingObject::GetCenterPos(TrackPoint32f * p)
{
	p->x = m_vCenter.x;
	p->y = m_vCenter.y;

	p->x = m_vCenter.x / m_iImgSize[0];
	p->y = m_vCenter.y / m_iImgSize[1];

	p->v = m_vCenter.v;

	
}

void CTrackingObject::GetMotion(TrackPoint32f* p)
{
	p->x = m_vMotion.x;
	p->y = m_vMotion.y;
}

BOOL CTrackingObject::Tracking(TrackPyramid* pPrev, TrackPyramid* pCur, COptFlowPyrLK* pkernel, int nFrameNo)
{
	if(!pkernel) 
		return FALSE;

	IppiPoint_32f prevPt[10];
	IppiPoint_32f nextPt[10];
	Ipp8s status[10];
	Ipp32f fError[10];

	int numfeat = m_nTPNum;
	
	for(int i = 0; i < numfeat; i++)
	{
		prevPt[i].x = m_TP[i].x;
		prevPt[i].y = m_TP[i].y;

		nextPt[i] = prevPt[i];
	}

	pkernel->Run(pPrev, pCur, prevPt, nextPt, numfeat, status, fError, nFrameNo);
	
	const float threshold_upper = (pkernel->GetSearchWindowSize() > 20 ? 25.0f : 15.0f);
	const float threshold_lower = 0.3f;
	int nImageWidth = 0, nImageHeight = 0;
	pkernel->GetImageSize(nImageWidth, nImageHeight);

	TrackPoint32f center;
	center.x = center.y = center.v = 0;
	
	int k = 0;
	for(int i = 0; i < numfeat; i++)
	{
		m_TP[i].x = fClip3(0.001f, (float)nImageWidth-0.001f, nextPt[i].x);
		m_TP[i].y = fClip3(0.001f, (float)nImageHeight-0.001f, nextPt[i].y);
		m_TP[i].v = fError[i]; //The square root of the average squared difference for each point

		if (status[i] != 0 && fError[i] < threshold_lower) //can't find feature point and search window became empty
		{
			m_TP[i].x = 0.0f;
			m_TP[i].y = 0.0f;
			m_TP[i].v = 0.0f;

            k++;
		}
		else //status[i] == 0 || fError[i]
		{
			if (status[i] == 0 && (fError[i] < threshold_upper && fError[i] >= threshold_lower)) //found feature point
			{
				if (abs(m_TP[i].x-prevPt[i].x)+abs(m_TP[i].y-prevPt[i].y) > ((m_fDimW*(float)nImageWidth) + (m_fDimH*(float)nImageHeight))*2.0f)
				{
					m_TP[i].x = 0.0f;
					m_TP[i].y = 0.0f;

                    k++;
				}
				else
				{
					center.x += m_TP[i].x;
					center.y += m_TP[i].y;
					center.v += 1.0f;
				}	
			}
			else
			{
				m_TP[i].x = 0.0f;
				m_TP[i].y = 0.0f;

                if(fError[i] >= threshold_upper)
                    k++;
			}
		}
	}
	
	// all feature point lost
	if(center.v < 1.0f)
	{
		OutputDebugStringA("!! All feature point lost !!");

		// When all points lost, using previous motion vector to decide current points.
		center.x = center.y = center.v = 0.0f;
		for (int i=0; i<numfeat; i++)
		{
			m_TP[i].x = prevPt[i].x - (fError[i]<0.0000001f ? 0.0f : m_vMotion.x);
			m_TP[i].y = prevPt[i].y - (fError[i]<0.0000001f ? 0.0f : m_vMotion.y);

			m_TP[i].x = fClip3(0.001f, (float)nImageWidth-0.001f, m_TP[i].x);
			m_TP[i].y = fClip3(0.001f, (float)nImageHeight-0.001f, m_TP[i].y);

			center.x += m_TP[i].x;
			center.y += m_TP[i].y;
		}

		m_vCenter.x = center.x/(float)numfeat;
		m_vCenter.y = center.y/(float)numfeat;

        if(k == numfeat)
            return FALSE;
        else
		    return TRUE;
	}

	TrackPoint32f vLostMotion; vLostMotion.x = vLostMotion.y = vLostMotion.v = 0.0f;

	// if has motion vector, and then update m_vMotion, otherwise not
	if (center.v > 2.0 && abs(m_vCenter.x - (center.x/center.v)) > 0.001 && abs(m_vCenter.y - (center.y/center.v)) > 0.001) // it should need over 2 points valid and then can use current motion vector
	{
		TrackPoint32f vFoundMotion; vFoundMotion.x = vFoundMotion.y = vFoundMotion.v = 0.0f;
		TrackPoint32f vFoundCenter; vFoundCenter.x = vFoundCenter.y = vFoundCenter.v = 0.0f;

		for (int i=0; i<numfeat; i++)
		{
			if (m_TP[i].x > 0.001f)
			{
				vFoundMotion.x += (prevPt[i].x - m_TP[i].x);
				vFoundMotion.y += (prevPt[i].y - m_TP[i].y);
				vFoundMotion.v += 1.0f;
			}
			else
			{
				vLostMotion.x += prevPt[i].x;
				vLostMotion.y += prevPt[i].y;
				vLostMotion.v += 1.0f;
			}
		}
		vFoundMotion.x /= vFoundMotion.v;
		vFoundMotion.y /= vFoundMotion.v;
		vFoundCenter.x = m_vCenter.x - vFoundMotion.x;
		vFoundCenter.y = m_vCenter.y - vFoundMotion.y;

		m_vMotion.x = m_vCenter.x - vFoundCenter.x;
		m_vMotion.y = m_vCenter.y - vFoundCenter.y;

		if (m_nSearchPattern == SEARCH_PATTERN_PNT)
		{
			vLostMotion.x = m_vMotion.x;
			vLostMotion.y = m_vMotion.y;
		}
		else
		{
			vLostMotion.x /= vLostMotion.v;
			vLostMotion.y /= vLostMotion.v;
			vLostMotion.x = (vLostMotion.x - (vFoundCenter.x*2.0f - (center.x/center.v)));
			vLostMotion.y = (vLostMotion.y - (vFoundCenter.y*2.0f - (center.y/center.v)));
		}
	}
	else
	{
		vLostMotion.x = m_vMotion.x;
		vLostMotion.y = m_vMotion.y;
	}

	m_vCenter.v = center.v / numfeat;

	// When feature points lost, select new points by using current motion vector.
	float vMotion[10];
	memset(vMotion, 0, 10*sizeof(float));
	for (int i=0; i<numfeat; i++)
	{
		if (m_TP[i].x < 0.001f)
		{
			m_TP[i].x = prevPt[i].x - (fError[i]<0.00000001f ? 0.0f : vLostMotion.x);
			m_TP[i].y = prevPt[i].y - (fError[i]<0.00000001f ? 0.0f : vLostMotion.y);

			m_TP[i].x = fClip3(0.001f, (float)nImageWidth-0.001f, m_TP[i].x);
			m_TP[i].y = fClip3(0.001f, (float)nImageHeight-0.001f, m_TP[i].y);

			center.x += m_TP[i].x;
			center.y += m_TP[i].y;
		}

		vMotion[i] = abs(prevPt[i].x - m_TP[i].x) + abs(prevPt[i].y - m_TP[i].y);
	}
	int nFeatureIdx = 0;
	float maxev = vMotion[nFeatureIdx];
	for (int i=1; i<numfeat; i++)
	{
		if (vMotion[i] > maxev)
			nFeatureIdx = i;
	}
	if (vMotion[nFeatureIdx] - vMotion[(nFeatureIdx+1)%numfeat] > (((m_fDimW*(float)nImageWidth) + (m_fDimH*(float)nImageHeight)) / 2.0f))
	{
		center.x -= m_TP[nFeatureIdx].x;
		center.y -= m_TP[nFeatureIdx].y;
		m_TP[nFeatureIdx].x = prevPt[nFeatureIdx].x - m_vMotion.x;
		m_TP[nFeatureIdx].y = prevPt[nFeatureIdx].y - m_vMotion.y;

		m_TP[nFeatureIdx].x = fClip3(0.001f, (float)nImageWidth-0.001f, m_TP[nFeatureIdx].x);
		m_TP[nFeatureIdx].y = fClip3(0.001f, (float)nImageHeight-0.001f, m_TP[nFeatureIdx].y);

		center.x += m_TP[nFeatureIdx].x;
		center.y += m_TP[nFeatureIdx].y;
	}

	// adjust new center point
	center.x /= (float)numfeat;
	center.y /= (float)numfeat;
	m_vMotion.x = m_vCenter.x - center.x;
	m_vMotion.y = m_vCenter.y - center.y;
	m_vCenter.x = center.x;
	m_vCenter.y = center.y;

	return TRUE;
}

BOOL CTrackingObject::GetTrackImage(TrackImage* pImage, TrackPyramid* pPyr, int nLevel)
{
	if(nLevel > pPyr->level)
		return FALSE;
	
	Ipp8u** pImg = pPyr->pImage;
	int * pStep = pPyr->pStep;
	IppiSize *pRoi = pPyr->pRoi;
	
	pImage->pImage = pPyr->pImage[nLevel];
	pImage->nPitch = pPyr->pStep[nLevel];
	pImage->size = pPyr->pRoi[nLevel];
	pImage->nBytePerPixel = 1;

	return TRUE;
}

COptFlowPyrLK::COptFlowPyrLK()
{
	m_pOF = 0;
}

COptFlowPyrLK::~COptFlowPyrLK()
{
	Close();
}


IppiPyramid* COptFlowPyrLK::AllocImagePyr(IppiSize roiSize, int numLevel)
{
	IppiPyramid* pPyr;
	float rate = 2.0f;
	Ipp16s pKernel[] = {1,4,6,4,1};
	int kerSize = 5;
	ippiPyramidInitAlloc(&pPyr, numLevel, roiSize, rate);

	IppiPyramidDownState_8u_C1R ** pState = (IppiPyramidDownState_8u_C1R**)&(pPyr->pState);
	Ipp8u** pImg = pPyr->pImage;
	int * pStep = pPyr->pStep;
	IppiSize *pRoi = pPyr->pRoi;
	int i, level;
	level = pPyr->level;

	ippiPyramidLayerDownInitAlloc_8u_C1R(pState,roiSize,rate,pKernel, kerSize, IPPI_INTER_LINEAR);

	for(i = 0; i <=level; i++)
	{
		pPyr->pImage[i] = ippiMalloc_8u_C1(pRoi[i].width, pRoi[i].height,pStep+i);
	}

	return pPyr;
}


void COptFlowPyrLK::UpdateImagePyr(IppiPyramid* pPyr,Ipp8u* pImage,int iStep, IppiSize roiSize)
{

	IppiPyramidDownState_8u_C1R * pState = (IppiPyramidDownState_8u_C1R*)(pPyr->pState);
	Ipp8u** pImg = pPyr->pImage;
	int * pStep = pPyr->pStep;
	IppiSize *pRoi = pPyr->pRoi;

	float rate = 2.0f;
	Ipp16s pKernel[] = {1,4,6,4,1};
	int kerSize = 5;

	
	pImg[0] = pImage;
	pStep[0] = iStep;
	pRoi[0] = roiSize;

	int level = pPyr->level;
	for(int i = 1; i <=level; i++)
	{
		ippiPyramidLayerDown_8u_C1R(pImg[i-1],pStep[i-1],pRoi[i-1],pImg[i],pStep[i],pRoi[i],pState);
	}

}



void COptFlowPyrLK::UpdateImagePyrRGB2Gray(BYTE* pRGB, int nPitch, TrackPyramid* pPyr)
{
	IppiPyramidDownState_8u_C1R * pState = (IppiPyramidDownState_8u_C1R*)(pPyr->pState);
	Ipp8u** pImg = pPyr->pImage;
	int * pStep = pPyr->pStep;
	IppiSize *pRoi = pPyr->pRoi;

	Ipp32f coeffs[3] = {0.114f, 0.587f, 0.299f};

	ippiColorToGray_8u_AC4C1R(pRGB, nPitch, pImg[0], pStep[0], pRoi[0], coeffs);

	float rate = 2.0f;
	Ipp16s pKernel[] = {1,4,6,4,1};
	int kerSize = 5;



	int level = pPyr->level;
	for(int i = 1; i <=level; i++)
	{
		ippiPyramidLayerDown_8u_C1R(pImg[i-1],pStep[i-1],pRoi[i-1],pImg[i],pStep[i],pRoi[i],pState);
	}

}




void COptFlowPyrLK::FreeImagePyr(IppiPyramid* p)
{
	int level = p->level;
	int i;

	for(i = level; i > 0; i--)
	{
		if(p->pImage[i]) ippiFree(p->pImage[i]);
	}
	ippiPyramidLayerDownFree_8u_C1R((IppiPyramidDownState_8u_C1R*)(p->pState));

	ippiPyramidFree(p);
}

void COptFlowPyrLK::Init(IppiSize roiSize, int winsize, int numIter, float threshold)
{
	IppHintAlgorithm hint = ippAlgHintFast;

	IppStatus status = ippiOpticalFlowPyrLKInitAlloc_8u_C1R(&m_pOF, roiSize, winsize, hint);
	m_winSize = winsize;
	m_numIter = numIter;
	m_threshold = threshold;
	m_roiSize = roiSize;

}

void COptFlowPyrLK::GetImageSize(int& x, int& y)
{
	x = m_roiSize.width;
	y = m_roiSize.height;
}

void COptFlowPyrLK::Run(IppiPyramid * pPyr1, IppiPyramid* pPyr2, 
						IppiPoint_32f* prevPt,IppiPoint_32f* nextPt, int numfeat, Ipp8s *pstatus, Ipp32f * pError, int nFrameNo)
{
	int level = pPyr1->level;

#if defined(_DBGLOG_OPTICAL_FLOW_)
	char szFileName[64];
	sprintf_s(szFileName, "C:\\OpticalFlowPyrLK.txt");

	FILE *fpPathInfo;
	fopen_s(&fpPathInfo, szFileName, "a");

	fprintf(fpPathInfo,"**[%d]Before prevPt: %04f %04f %04f %04f %04f, FeaNum: %d, WinSize: %d, Level: %d, IterNum: %d, Threshold: %08f\n", nFrameNo,
		prevPt[0].x, prevPt[1].x, prevPt[2].x, prevPt[3].x, prevPt[4].x, numfeat, m_winSize, level, m_numIter, m_threshold);
#endif

	IppStatus t= ippiOpticalFlowPyrLK_8u_C1R(pPyr1, pPyr2, prevPt, nextPt, pstatus, pError,
		numfeat, m_winSize, level, m_numIter, m_threshold, m_pOF);

#if defined(_DBGLOG_OPTICAL_FLOW_)
	fprintf(fpPathInfo,"**[%d]After nextPt: %04f %04f %04f %04f %04f, Status: %d %d %d %d %d Error: %04f %04f %04f %04f %04f\n\n", nFrameNo,
		nextPt[0].x, nextPt[1].x, nextPt[2].x, nextPt[3].x, nextPt[4].x, pstatus[0], pstatus[1], pstatus[2], pstatus[3], pstatus[4], pError[0], pError[1], pError[2], pError[3], pError[4]);
	fclose(fpPathInfo);
#endif
}

void COptFlowPyrLK::Close()
{
	ippiOpticalFlowPyrLKFree_8u_C1R(m_pOF);
}


//////////////////////////////////////////////////////////////////////////
static void SobelGradient(Ipp8u* pImage, int iStep, int x, int y, IppiPoint_32f* pGrad)
{
	Ipp8u pixel[9];
	int idx = (y-1)*iStep + x - 1;
	pixel[0] = pImage[idx];
	pixel[1] = pImage[idx+1];
	pixel[2] = pImage[idx+2];

	idx += iStep;
	pixel[3] = pImage[idx];
	pixel[4] = pImage[idx+1];
	pixel[5] = pImage[idx+2];

	idx += iStep;
	pixel[6] = pImage[idx];
	pixel[7] = pImage[idx+1];
	pixel[8] = pImage[idx+2];


	pGrad->x = ((pixel[0] - pixel[2]) + 2 * (pixel[3] - pixel[5]) + (pixel[6] - pixel[8]))* 0.25f;
	pGrad->y = ((pixel[0] - pixel[6]) + 2 * (pixel[1] - pixel[7]) + (pixel[2] - pixel[8]))* 0.25f;

}
static float minEigenValue(float gxx, float gxy, float gyy )
{
	return (float)((gxx + gyy - sqrt((gxx - gyy)*(gxx - gyy) + 4.0f * gxy*gxy))/2.0f);
}

//////////////////////////////////////////////////////////////////////////
CTrackingPath::CTrackingPath()
{
	Reset();
}

CTrackingPath::~CTrackingPath()
{
	m_vecPath.clear();
}

int CTrackingPath::CheckPathRange(int iKey)
{
	if(iKey >= m_nPathIn && iKey <= m_nPathOut)
		return 1;
	else if(m_nPathIn < 0)
		return 0;
	else
		return -1;
}

void CTrackingPath::Reset()
{
	m_vecPath.clear();
	m_nPathOut = m_nPathIn = m_nInitKey = -1;
}

int CTrackingPath::GetPathDuration()
{
	int t =  m_nPathOut - m_nPathIn;
	if( t <= 0 )
		return 0;

	return t + 1;
}

int CTrackingPath::GetPathPos(int iKey, TrackPoint32f* p)
{
	if(iKey < m_nPathIn)
		iKey = m_nPathIn;
	if(iKey > m_nPathOut)
		iKey = m_nPathOut;


	if(iKey >= m_nPathIn && iKey <= m_nPathOut)
	{
		TrackPoint32f& pt = m_vecPath.at(iKey - m_nPathIn);
		p->x = pt.x;
		p->y = pt.y;

		p->v = pt.v;

		return 1;
	}


	return 0;
}

int CTrackingPath::GetPathOffset(int iKey, TrackPoint32f* p)
{
	if(iKey < m_nPathIn)
		iKey = m_nPathIn;
	if(iKey > m_nPathOut)
		iKey = m_nPathOut;
	
	if(iKey < 0)
		return -1;

	TrackPoint32f basePt = m_vecPath[0];

	if(iKey >= m_nPathIn && iKey <= m_nPathOut)
	{
		TrackPoint32f& pt = m_vecPath.at(iKey - m_nPathIn);
		p->x = pt.x - basePt.x;
		p->y = pt.y - basePt.y;

		return 1;
	}


	return 0;
}

void CTrackingPath::SetPathRange(int nInPos, int nOutPos)
{
	Reset();

	m_nPathIn = nInPos;
	m_nPathOut = nOutPos;
}

void CTrackingPath::SetPathPos(int iKey, TrackPoint32f* p)
{
	if(m_nInitKey < 0)
		m_nInitKey = iKey;

	if(m_nPathIn < 0) // no path
	{
		m_nPathIn = iKey;
		m_nPathOut = iKey;
		m_vecPath.push_back(*p);
		return;
	}
	
	if(iKey >= m_nPathIn && iKey <= m_nPathOut)
	{
		m_vecPath.push_back(*p);
		return;
	}

	if(iKey > m_nPathOut)
	{
		m_vecPath.push_back(*p);
		m_nPathOut = iKey;
		return;
	}
}

size_t CTrackingPath::GetCurrentPathLength()
{
	return m_vecPath.size();
}

void CTrackingPath::PieceWiseQuadraticBezier(int iKeyIn, int iKeyOut, float& ctlx, float& ctly)
{
	if(iKeyIn < m_nPathIn || iKeyIn > m_nPathOut)
		return ;

	if(iKeyOut < m_nPathIn || iKeyOut > m_nPathOut)
		return;
	
	TrackPoint32f& pt0 = m_vecPath.at(iKeyIn - m_nPathIn);
	TrackPoint32f& pt1 = m_vecPath.at(iKeyOut - m_nPathIn);
	

	// line equ:
	float line_equ[3];
	line_equ[0] = pt0.y - pt1.y;
	line_equ[1] = pt1.x - pt0.x;
	line_equ[2] = pt0.x * pt1.y - pt1.x * pt0.y;

	// max and min dist
	float dist_max, dist_min;
	dist_max = dist_min = 0;

	int dist_max_k = 0;
	int dist_min_k = 0;

	for(int k = iKeyIn; k <= iKeyOut; k++)
	{
		TrackPoint32f& t = m_vecPath[k - m_nPathIn];

		float dist = line_equ[0] * t.x + line_equ[1] * t.y + line_equ[2];
		if(dist_max < dist){
			dist_max = dist;
			dist_max_k = k;
		}

		if(dist_min > dist){ 
			dist_min = dist;
			dist_min_k = k;
		}

	}
	
	int peak_k;

	if(abs(dist_max) >= abs(dist_min))
		peak_k = dist_max_k;
	else
		peak_k = dist_min_k;


	TrackPoint32f& peak = m_vecPath.at(peak_k - m_nPathIn);
	float t = 1.0f * (peak_k - iKeyIn)/(iKeyOut - iKeyIn);
	float rt = 1-t;
	float rt2 = rt * rt;
	float t2 = t * t;
	float trt2 = 2 * rt * t;

	ctlx = (peak.x - rt2 * pt0.x - t2 * pt1.x )/trt2;
	ctly = (peak.y - rt2 * pt0.y - t2 * pt1.y )/trt2;


	ctlx = (ctlx + (pt0.x + pt1.x) * 0.5f) * 0.5f;
	ctly = (ctly + (pt0.y + pt1.y) * 0.5f) * 0.5f;

}
