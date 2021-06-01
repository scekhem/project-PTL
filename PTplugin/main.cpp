#include "PTplugin.h"
#include <QtWidgets/QApplication>

int main(int argc, char *argv[])
{
	std::vector<std::string> full_path = { "../test.ptcloud" };
	PtCloud container;
	QApplication a(argc, argv);
	PTplugin* w;
	container = restoreCloudDataBinary(full_path[0]);
	for (int i = 0; i < 5; i++) {

		w = new PTplugin();
		w->setData(container);
		w->exec();
		delete w;
	}
	return a.exec();
}
