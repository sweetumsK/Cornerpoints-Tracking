#pragma once
#include<opencv2\opencv.hpp>
#include<vector>

typedef std::vector<cv::Point2f> PointList;

class CornerFinding {
public:
	PointList static GetCorner(const cv::Mat& inImage);
private:
	CornerFinding(const cv::Mat& inImage);
	~CornerFinding();
	PointList GetShiTomasiCorner();

	cv::Mat m_Image;
	PointList m_CornerPoints;
};
