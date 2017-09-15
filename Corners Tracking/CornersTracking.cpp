#include"CornersFinding.h"
#include"CornersTracking.h"

void CornerTracking::operate(cv::VideoCapture& cap, std::string videoName) {
	CornerTracking cornerTracking(cap, videoName);
	cornerTracking.getCornerTrackingVideo();
}

CornerTracking::CornerTracking(cv::VideoCapture& cap, std::string videoName)
	:m_cap(cap) 
{
	m_FourCC = cap.get(CV_CAP_PROP_FOURCC);
	m_frameFPS = cap.get(CV_CAP_PROP_FPS);
	m_frameWidth = static_cast<int>(cap.get(CV_CAP_PROP_FRAME_WIDTH));
	m_frameHeight = static_cast<int>(cap.get(CV_CAP_PROP_FRAME_HEIGHT));
	m_frameCount = static_cast<int>(cap.get(CV_CAP_PROP_FRAME_COUNT));
	m_video.open(videoName, m_FourCC, m_frameFPS, cv::Size(m_frameWidth,m_frameHeight));
}

void CornerTracking::getCornerTrackingVideo() {
	convertVideoToRGBA();
	getCornerPoints();
	drawCornerPoints();
	converRGBAToVideo();
	InitTrackingManager(m_frameWidth, m_frameHeight, m_currentImage.step);
	StartTracker(m_pBuffer, m_trackPoints, m_ptsCount);
	for (int i = 1; i < m_frameCount; i++) {
		convertVideoToRGBA();
		getPointsByOpticalFlow();
		converRGBAToVideo();
	}
}

void  CornerTracking::getCornerPoints() {
	m_cornerPoints = CornerFinding::GetCorner(m_currentImage);
	m_ptsCount = m_cornerPoints.size();
	converCornerPointsToTrackPoints();
}

void  CornerTracking::getPointsByOpticalFlow() {
	const int PARAM_POINTS_COUNT = m_ptsCount;
	TrackPoint*pts  = new TrackPoint[PARAM_POINTS_COUNT];
	GoTracker(m_pBuffer, pts, m_ptsCount);
	converTrackPointsToCornerPoints();
}

void CornerTracking::converCornerPointsToTrackPoints() {
	const int PARAM_POINTS_COUNT = m_ptsCount;
	m_trackPoints = new TrackPoint[PARAM_POINTS_COUNT];
	for (int i = 0; i < m_ptsCount; ++i) {
		m_trackPoints[i].x = m_cornerPoints[i].x;
		m_trackPoints[i].y = m_cornerPoints[i].y;
	}
}

void CornerTracking::converTrackPointsToCornerPoints() {
	m_cornerPoints.clear();
	for (int i = 0; i < m_ptsCount; ++i)
		m_cornerPoints.push_back(cv::Point2f(m_trackPoints[i].x, m_trackPoints[i].y));
}

void  CornerTracking::drawCornerPoints() {
	for (size_t point = 0; point < m_ptsCount; ++point)
		circle(m_currentImage, m_cornerPoints[point], 3, cv::Scalar(0, 255, 255));
}

void CornerTracking::convertVideoToRGBA() {
	cv::Mat temp;
	m_cap >> temp;
	cvtColor(temp, m_currentImage, CV_RGB2RGBA);
	delete(m_pBuffer);
	const int size = m_currentImage.step*m_currentImage.rows;
	m_pBuffer = new BYTE[size];
	std::memcpy(m_pBuffer, m_currentImage.data, size);
	cv::imshow("test", m_currentImage);
	cv::waitKey(0);
}

void CornerTracking::converRGBAToVideo() {
	cv::imshow("test", m_currentImage);
	cv::waitKey(0);
	m_video << m_currentImage;
}
