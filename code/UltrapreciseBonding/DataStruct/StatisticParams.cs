namespace DataStruct
{
    /// <summary>
    /// 统计参数
    /// </summary>
    public class DataStatisticParam
    {
        private double _mean;       // 均值
        private double _sigma3;     // 3σ
        private double _meanSigma3; // 均值+3σ
        private double _min;        // 最小值
        private double _max;        // 最大值
        private double _median;     // 中值
        private double _range;      // 极差值

        /// <summary>
        /// 默认构造
        /// </summary>
        public DataStatisticParam()
        {
            _mean = 0.0;
            _sigma3 = 0.0;
            _meanSigma3 = 0.0; // 均值+3σ
            _min = 0.0;        // 最小值
            _max = 0.0;        // 最大值
            _median = 0.0;     // 中值
            _range = 0.0;      // 极差值
        }

        /// <summary>
        /// Gets or sets the user's _mean
        /// </summary>
        public double Mean { get => _mean; set => _mean = value; }

        /// <summary>
        /// Gets or sets the user's _sigma3
        /// </summary>
        public double Sigma3 { get => _sigma3; set => _sigma3 = value; }

        /// <summary>
        /// Gets or sets the user's _meanSigma3
        /// </summary>
        public double MeanSigma3 { get => _meanSigma3; set => _meanSigma3 = value; }

        /// <summary>
        /// Gets or sets the user's _min
        /// </summary>
        public double Min { get => _min; set => _min = value; }

        /// <summary>
        /// Gets or sets the user's _max
        /// </summary>
        public double Max { get => _max; set => _max = value; }

        /// <summary>
        /// Gets or sets the user's _median
        /// </summary>
        public double Median { get => _median; set => _median = value; }

        /// <summary>
        /// Gets or sets the user's _range
        /// </summary>
        public double Range { get => _range; set => _range = value; }
    }

    /// <summary>
    /// AVM 参数
    /// </summary>
    public class AVMOverlayModelParam
    {
        private double _transX;         // x向偏移量
        private double _transY;         // y向偏移量
        private double _rotationUradUrad;       // 旋转量（单位 urad）
        private double _runOut;         // 放大倍率(单位 ppm，百万分比)
        private double _goodnessOfFit;  // 拟合优度（单位%）
        private double _modelError;     // 模型误差（单位和偏移量相同）

        /// <summary>
        /// 默认构造
        /// </summary>
        public AVMOverlayModelParam()
        {
            _transX = 0.0;
            _transY = 0.0;
            _rotationUradUrad = 0.0;
            _runOut = 0.0;
            _goodnessOfFit = 0.0;
            _modelError = 0.0;
        }

        /// <summary>
        /// Gets or sets the user's X向偏移量
        /// </summary>
        public double TransX { get => _transX; set => _transX = value; }

        /// <summary>
        /// Gets or sets the user's Y向偏移量
        /// </summary>
        public double TransY { get => _transY; set => _transY = value; }

        /// <summary>
        /// Gets or sets the user's 偏转角 单位：urad 微弧度
        /// </summary>
        public double RotationUrad { get => _rotationUradUrad; set => _rotationUradUrad = value; }

        /// <summary>
        /// Gets or sets the user's 缩放量(单位 ppm，百万分比)
        /// </summary>
        public double RunOut { get => _runOut; set => _runOut = value; }

        /// <summary>
        /// Gets or sets the user's overlay拟合优度
        /// </summary>
        public double GoodnessOfFit { get => _goodnessOfFit; set => _goodnessOfFit = value; }

        /// <summary>
        /// Gets or sets the user's overlay拟合残差
        /// </summary>
        public double ModelError { get => _modelError; set => _modelError = value; }
    }
}