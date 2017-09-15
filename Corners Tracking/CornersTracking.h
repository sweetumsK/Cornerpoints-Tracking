#pragma once
#include<opencv2\opencv.hpp>
#include<vector>
#include<string>
#include<windows.h>
#include"ObjTrackCore.h"
#include"TrackingManager.h"

#pragma comment(lib, "TrackingManager.lib")
extern "C" __declspec(dllimport) ITrackingManager* GetTrackingManager();
extern "C" __declspec(dllimport) void InitTrackingManager(int nWidth, int nHeight, int nPitch);
extern "C" __declspec(dllimport) void StartTracker(BYTE *pBuffer, TrackPoint pts[], int size);
extern "C" __declspec(dllimport) void GoTracker(BYTE *pBuffer, TrackPoint pts[], int &size);

typedef std::vector<cv::Point2f> PointList;
class CornerTracking {
public:
	static void operate(cv::VideoCapture& cap, std::string videoName);
private:
	CornerTracking(cv::VideoCapture& cap, std::string videoName);
	virtual ~CornerTracking() {};
	void getCornerTrackingVideo();
	void getCornerPoints();
	void getPointsByOpticalFlow();
	void converCornerPointsToTrackPoints();
	void converTrackPointsToCornerPoints();
	void drawCornerPoints();
	void convertVideoToRGBA();
	void converRGBAToVideo();

	cv::VideoCapture m_cap;
	double m_FourCC, m_frameFPS;
	int m_frameWidth, m_frameHeight;

	int m_frameCount;
	cv::Mat m_currentImage;
	BYTE* m_pBuffer;

	int m_ptsCount;
	PointList m_cornerPoints;
	TrackPoint* m_trackPoints;

	cv::VideoWriter m_video;
};