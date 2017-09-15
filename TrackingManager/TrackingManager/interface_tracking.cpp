#include "stdafx.h"
#include "ObjTrackCore.h"
#include "TrackingManager.h"

#define center_size 11.0

static CTrackingManager *s_pTrackingManager;
static int s_nWidth = 0;
static int s_nHeight = 0;
static int s_nPitch = 0;

extern "C" __declspec(dllexport) ITrackingManager* GetTrackingManager()
{
    if(!s_pTrackingManager)
        s_pTrackingManager = new CTrackingManager(0);
    return s_pTrackingManager;
}

extern "C" __declspec(dllexport) void InitTrackingManager(int nWidth, int nHeight, int nPitch)
{
    if(s_pTrackingManager)
        delete s_pTrackingManager;
    s_pTrackingManager = new CTrackingManager(0);
    s_pTrackingManager->InitTrackPyramid(nWidth, nHeight, 0);

    s_nWidth = nWidth;
    s_nHeight = nHeight;
    s_nPitch = nPitch;
}

extern "C" __declspec(dllexport) void StartTracker(BYTE *pBuffer, TrackPoint pts[], int size)
{
    s_pTrackingManager->UpdateTrackPyramid(pBuffer, s_nPitch, 0);

    int count = s_pTrackingManager->GetTrackerCount();
    int k = size - s_pTrackingManager->GetTrackerCount();
    if(k > 0)
    {
        for(int i = 0; i < k; i++)
        {
            s_pTrackingManager->AddTracker(count + i, NULL);
        }
    }

    for(int i = 0; i < size; i++)
    {
        MotionTracker* pTracker = s_pTrackingManager->GetTracker(i);
        pTracker->SetFeaturePos(pts[i].x, pts[i].y);
	    pTracker->SetFeatureDims(center_size / s_nWidth, center_size / s_nHeight, true);
        s_pTrackingManager->StartTracker(pTracker, 0);
    }
}

extern "C" __declspec(dllexport) void GoTracker(BYTE *pBuffer, TrackPoint pts[], int &size)
{
    s_pTrackingManager->UpdateTrackPyramid(pBuffer, s_nPitch, 0);

    int count = s_pTrackingManager->GetTrackerCount();
    int k = size <= count ? size : count;
    size = 0;
    for(int i = 0; i < k; i++)
    {
        MotionTracker* pTracker = s_pTrackingManager->GetTracker(i);
        BOOL ret = s_pTrackingManager->GoTracker(pTracker, 0);
        if(ret > 0)
        {
		    pTracker->GetCurPos(pts[size].x, pts[size].y);
            size++;
        }
    }
}