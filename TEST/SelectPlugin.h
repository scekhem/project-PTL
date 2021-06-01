#pragma once

#define VIS_PLUGIN __declspec(dllimport)  

#include <QtWidgets/QWidget>
#include <ptcloud.h>
#include <ptframe.h>
#include <frameio.h>


class  SelectPlugin : public QWidget
{
    Q_OBJECT

public:
    SelectPlugin(QWidget *parent = Q_NULLPTR);
	~SelectPlugin();

signals:
	void sendData(std::vector<double> &pointdata);

public:
	std::vector<double> getData();

};
