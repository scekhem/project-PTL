/********************************************************************************
** Form generated from reading UI file 'PTplugin.ui'
**
** Created by: Qt User Interface Compiler version 5.9.8
**
** WARNING! All changes made in this file will be lost when recompiling UI file!
********************************************************************************/

#ifndef UI_PTPLUGIN_H
#define UI_PTPLUGIN_H

#include <QtCore/QVariant>
#include <QtWidgets/QAction>
#include <QtWidgets/QApplication>
#include <QtWidgets/QButtonGroup>
#include <QtWidgets/QDialog>
#include <QtWidgets/QFrame>
#include <QtWidgets/QGridLayout>
#include <QtWidgets/QHBoxLayout>
#include <QtWidgets/QHeaderView>
#include <QtWidgets/QLineEdit>
#include <QtWidgets/QPushButton>
#include "QVTKWidget.h"

QT_BEGIN_NAMESPACE

class Ui_PTpluginClass
{
public:
    QGridLayout *gridLayout;
    QVTKWidget *points_viewer;
    QFrame *frame;
    QHBoxLayout *horizontalLayout;
    QLineEdit *lineEdit;
    QPushButton *confirm_btn;

    void setupUi(QDialog *PTpluginClass)
    {
        if (PTpluginClass->objectName().isEmpty())
            PTpluginClass->setObjectName(QStringLiteral("PTpluginClass"));
        PTpluginClass->resize(923, 520);
        gridLayout = new QGridLayout(PTpluginClass);
        gridLayout->setSpacing(6);
        gridLayout->setContentsMargins(11, 11, 11, 11);
        gridLayout->setObjectName(QStringLiteral("gridLayout"));
        points_viewer = new QVTKWidget(PTpluginClass);
        points_viewer->setObjectName(QStringLiteral("points_viewer"));

        gridLayout->addWidget(points_viewer, 0, 0, 1, 1);

        frame = new QFrame(PTpluginClass);
        frame->setObjectName(QStringLiteral("frame"));
        frame->setMaximumSize(QSize(16777215, 55));
        frame->setFrameShape(QFrame::StyledPanel);
        frame->setFrameShadow(QFrame::Raised);
        horizontalLayout = new QHBoxLayout(frame);
        horizontalLayout->setSpacing(6);
        horizontalLayout->setContentsMargins(11, 11, 11, 11);
        horizontalLayout->setObjectName(QStringLiteral("horizontalLayout"));
        lineEdit = new QLineEdit(frame);
        lineEdit->setObjectName(QStringLiteral("lineEdit"));
        lineEdit->setMinimumSize(QSize(0, 30));
        lineEdit->setMaximumSize(QSize(16777215, 30));

        horizontalLayout->addWidget(lineEdit);

        confirm_btn = new QPushButton(frame);
        confirm_btn->setObjectName(QStringLiteral("confirm_btn"));
        confirm_btn->setMinimumSize(QSize(120, 40));

        horizontalLayout->addWidget(confirm_btn);


        gridLayout->addWidget(frame, 1, 0, 1, 1);


        retranslateUi(PTpluginClass);

        QMetaObject::connectSlotsByName(PTpluginClass);
    } // setupUi

    void retranslateUi(QDialog *PTpluginClass)
    {
        PTpluginClass->setWindowTitle(QApplication::translate("PTpluginClass", "PTplugin", Q_NULLPTR));
        confirm_btn->setText(QApplication::translate("PTpluginClass", "PushButton", Q_NULLPTR));
    } // retranslateUi

};

namespace Ui {
    class PTpluginClass: public Ui_PTpluginClass {};
} // namespace Ui

QT_END_NAMESPACE

#endif // UI_PTPLUGIN_H
