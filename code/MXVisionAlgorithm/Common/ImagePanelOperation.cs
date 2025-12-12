using DataStruct;
using MwFramework.Controls.ControlCanvas.DrawingControl;
using MwFramework.Controls.ControlCanvas.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MXVisionAlgorithm.Common
{
    public static class ImagePanelOperation
    {
        /// <summary>
        /// 绘制十字
        /// </summary>
        /// <param name="imagePanel">图形控件</param>
        /// <param name="centerPointT">十字中心点坐标</param>
        /// <param name="width">十字线宽度</param>
        public static void DrawCenterCross(DesignPanel imagePanel, System.Windows.Point centerPointT, int width = 5000)
        {
            //imagePanel.ShapeContent = new LineShapeModel();
            //var shapelineH = new PrimShape();
            //shapelineH.Region = new LineRegion();
            //System.Windows.Point centerPoint = new System.Windows.Point(centerPointT.X / imagePanel.WidthRatio, centerPointT.Y / imagePanel.HeightRatio);
            //System.Windows.Point startPointH = new System.Windows.Point(centerPoint.X - width, centerPoint.Y);
            //System.Windows.Point endPointH = new System.Windows.Point(centerPoint.X + width, centerPoint.Y);
            //imagePanel.ShapeContent.SetReportItem(imagePanel, shapelineH.Region, startPointH, endPointH);
            //imagePanel.Shapes.Add(shapelineH);
            //var shapelineV = new PrimShape();
            //shapelineV.Region = new LineRegion();
            //System.Windows.Point startPointV = new System.Windows.Point(centerPoint.X, centerPoint.Y - width);
            //System.Windows.Point endPointV = new System.Windows.Point(centerPoint.X, centerPoint.Y + width);
            //imagePanel.ShapeContent.SetReportItem(imagePanel, shapelineV.Region, startPointV, endPointV);
            //imagePanel.Shapes.Add(shapelineV);

            imagePanel.Items.Add(new LineShapeDrawing() { X = centerPointT.X - width, Y = centerPointT.Y, X1 = centerPointT.X + width, Y1 = centerPointT.Y, Range = 1000 });
            imagePanel.Items.Add(new LineShapeDrawing() { X = centerPointT.X, Y = centerPointT.Y - width, X1 = centerPointT.X, Y1 = centerPointT.Y + width });
        }

        /// <summary>
        /// 获取视窗内的矩形
        /// </summary>
        /// <param name="imagePanel"></param>
        /// <param name="rectangleList"></param>
        public static void GetViewRectangle(DesignPanel imagePanel, out List<Rectangle1> rectangleList)
        {
            rectangleList = new List<Rectangle1>();
            var shapes = imagePanel.Shapes;
            foreach (var item in shapes)
            {
                if (item.Region is RectRegion)
                {
                    Rectangle1 rect = new Rectangle1();
                    var tObj = item.Region as RectRegion;
                    rect.Start_X = (tObj.CenterPoint.X - tObj.Width / 2);
                    rect.Start_Y = (tObj.CenterPoint.Y - tObj.Height / 2);
                    rect.End_X = (tObj.CenterPoint.X + tObj.Width / 2);
                    rect.End_Y = (tObj.CenterPoint.Y + tObj.Height / 2);
                    rectangleList.Add(rect);
                }
            }
        }
    }
}
