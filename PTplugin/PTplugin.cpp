#include "PTplugin.h"
#include <pcl/common/common.h>
#include <pcl/point_types.h>
#include <pcl/visualization/cloud_viewer.h>
#include <vtkAutoInit.h>
#include <vtkRenderWindow.h>

void point_select_callback(const pcl::visualization::PointPickingEvent& event, void* args)
{
	PTplugin* temp_Widget = reinterpret_cast<PTplugin*>(args);
	temp_Widget->pointSelectCallback(event, (void*)& temp_Widget->cb_args);
	temp_Widget->cb_args->clicked_point->clear();
}

PTplugin::PTplugin(QWidget *parent)
    : QDialog(parent)
{
    ui.setupUi(this);
	VTK_MODULE_INIT(vtkRenderingOpenGL2);
	VTK_MODULE_INIT(vtkInteractionStyle);
	connect(ui.confirm_btn, SIGNAL(clicked(bool)), this, SLOT(confirm_clicked()));
	this->setWindowFlag(Qt::WindowContextHelpButtonHint, false);
	//this->setWindowFlag(Qt::WindowCloseButtonHint, false);
	this->setWindowFlag(Qt::WindowMaximizeButtonHint, true);
	//this->setAttribute(Qt::WA_DeleteOnClose, true);
	cloud_in.reset(new pcl::PointCloud< pcl::PointXYZ >());
	selected_points.reset(new pcl::PointCloud<pcl::PointXYZ>()); /// this will be replaced by " reload_selected_points ";
	clicked_point.reset(new pcl::PointCloud<pcl::PointXYZ>()); /// this will be replaced by " reload_selected_points ";
	pclviewer.reset(new pcl::visualization::PCLVisualizer("viewer", false));
	pclviewer->addPointCloud(cloud_in, "raw_cloud");
	pclviewer->getRenderWindow();
	pclviewer->setupInteractor(ui.points_viewer->GetInteractor(), ui.points_viewer->GetRenderWindow());
	ui.points_viewer->SetRenderWindow(pclviewer->getRenderWindow());

	pclviewer->registerPointPickingCallback(point_select_callback, this);
	
}

void PTplugin::confirm_clicked() {
	emit sendData(getData());
	//delete ui;
	//ui = nullptr;
	this->close();
}

PTplugin::~PTplugin()
{
	//delete ui;
	//delete cb_args;
	std::cout << "[close] \n";
}

void PTplugin::setData(const PtCloud & rawData)
{
	cloud_in.reset(new pcl::PointCloud< pcl::PointXYZ >());
	selected_points.reset(new pcl::PointCloud<pcl::PointXYZ>()); /// this will be replaced by " reload_selected_points ";
	clicked_point.reset(new pcl::PointCloud<pcl::PointXYZ>()); /// this will be replaced by " reload_selected_points ";

	cb_args->clicked_point = clicked_point;
	cb_args->viewerPtr = pclviewer;
	pcl::PointXYZ temp_point;

	cloud_in->points.clear();
	selected_points->points.clear();
	clicked_point->points.clear();

	std::vector<PtNode> pt_nodes = rawData.getNodes();
	if (pt_nodes.size() < 1) return ;
	for (int i = 0; i < pt_nodes.size(); i++)
	{
		if (pt_nodes[i].laser.hasNaN()) continue;
		else {
			temp_point.x = pt_nodes[i].laser.x();
			temp_point.y = pt_nodes[i].laser.y();
			temp_point.z = pt_nodes[i].laser.z();
			cloud_in->points.push_back(temp_point);
		}
	}
	pcl::visualization::PointCloudColorHandlerGenericField<pcl::PointXYZ> fildColor(cloud_in, "z");
	pcl::visualization::PointCloudColorHandlerCustom<pcl::PointXYZ> red(cb_args->clicked_point, 255, 0, 0);
	pcl::visualization::PointCloudColorHandlerCustom<pcl::PointXYZ> high_light(selected_points, 255, 255, 255);
	pclviewer->removeAllPointClouds();
	pclviewer->addPointCloud(cloud_in, fildColor, "raw_cloud");
	pclviewer->addPointCloud(clicked_point, red, "clicked_points");
	pclviewer->addPointCloud(selected_points, high_light, "selected_points");
	pclviewer->setPointCloudRenderingProperties(pcl::visualization::PCL_VISUALIZER_POINT_SIZE, 1, "raw_cloud");
	pclviewer->setPointCloudRenderingProperties(pcl::visualization::PCL_VISUALIZER_POINT_SIZE, 7, "selected_points");
	pclviewer->setPointCloudRenderingProperties(pcl::visualization::PCL_VISUALIZER_POINT_SIZE, 9, "clicked_points");
	pclviewer->resetCamera();


	ui.points_viewer->update();
	ui.lineEdit->setText("selected point >> NAN");
	return ;

}

void PTplugin::pointSelectCallback(const pcl::visualization::PointPickingEvent& event, void* args)
{
	callback_args* data = (callback_args*)args;
	if (event.getPointIndex() == -1)
		return;
	pcl::PointXYZ current_point;
	event.getPoint(current_point.x, current_point.y, current_point.z);
	clicked_point->clear();
	clicked_point->points.clear();
	clicked_point->points.push_back(current_point);
	selected_points->clear();
	selected_points->points.push_back(cb_args->clicked_point->points[0]);
	pcl::visualization::PointCloudColorHandlerCustom<pcl::PointXYZ> red(cb_args->clicked_point, 255, 0, 0);
	data->viewerPtr->updatePointCloud(cb_args->clicked_point, red, "clicked_points");
	selected_points->clear();
	selected_points->points.push_back(cb_args->clicked_point->points[0]);
	ui.lineEdit->clear();
	for (int i = 0; i < selected_points->points.size(); i++)
	{
		QString label_x = QString::number(double(selected_points->points[i].x), 10, 3);
		QString label_y = QString::number(double(selected_points->points[i].y), 10, 3);
		QString label_z = QString::number(double(selected_points->points[i].z), 10, 3);
		ui.lineEdit->setText("selected point >> x:" + label_x + "  y:" + label_y + "  z:" + label_z);
	}
	pclviewer->updatePointCloud(selected_points, "selected_points");
	ui.points_viewer->update();
	//this->clicked_point->points.clear();
}

std::vector<double> PTplugin::getData()
{
	std::vector<double> res;
	if (selected_points->points.size() < 1) return res;
	res.push_back(selected_points->points[0].x);
	res.push_back(selected_points->points[0].y);
	res.push_back(selected_points->points[0].z);
	return res;
}
