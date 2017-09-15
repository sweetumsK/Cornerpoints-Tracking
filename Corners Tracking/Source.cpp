#include "CornersTracking.h"

int main(void) {
	cv::VideoCapture cap("test.avi");
	CornerTracking::operate(cap, "result.avi");
	return 0;
}