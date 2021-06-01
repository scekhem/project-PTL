#pragma once

#define DLL_API __declspec(dllexport)  

#include <QtWidgets/QDialog>
#include "ui_PTplugin.h"
#include <stdio.h>
#include <iostream>

#include <ptcloud.h>
#include <ptframe.h>
#include <frameio.h>

#include <pcl/common/common.h>
#include <pcl/point_types.h>
#include <pcl/visualization/cloud_viewer.h>

struct callback_args
{
	pcl::PointCloud<pcl::PointXYZ>::Ptr clicked_point;
	boost::shared_ptr<pcl::visualization::PCLVisualizer> viewerPtr;
};

class DLL_API PTplugin : public QDialog
{
    Q_OBJECT

public:
	PTplugin(QWidget *parent = Q_NULLPTR);
	~PTplugin();

signals:
	void sendData(std::vector<double> &pointdata);

public:
	void setData(const PtCloud& rawData);
	void pointSelectCallback(const pcl::visualization::PointPickingEvent& event, void* args);
	std::vector<double> getData();
	struct callback_args* cb_args;
	pcl::PointCloud<pcl::PointXYZ>::Ptr cloud_in;
	pcl::PointCloud<pcl::PointXYZ>::Ptr clicked_point;
	pcl::PointCloud<pcl::PointXYZ>::Ptr selected_points;
	boost::shared_ptr<pcl::visualization::PCLVisualizer> pclviewer;
	
private:
    Ui::PTpluginClass ui;


private slots:
	void confirm_clicked();
};