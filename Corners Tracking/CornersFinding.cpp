#include"CornersFinding.h"

PointList CornerFinding::GetCorner(const cv::Mat& inImage) {
	CornerFinding cornerFinding(inImage);
	return cornerFinding.GetShiTomasiCorner();
}

CornerFinding::CornerFinding(const cv::Mat& inImage) {
	cvtColor(inImage, m_Image, CV_RGB2GRAY);
}

CornerFinding::~CornerFinding() {}

PointList CornerFinding::GetShiTomasiCorner() {
	goodFeaturesToTrack(m_Image, m_CornerPoints, 300, 0.01, 20, cv::Mat(), 3, false, 0.04);
	return m_CornerPoints;
}