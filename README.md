# TrainScheduling
列车运行图模拟 in WPF

### 软件架构说明

1. 用文件夹将各部分代码区分开: 

	 + /Algorithm 做算法和功能性代码
	 + /Model 放模型和类代码
	 + /Data 放模拟运行图时的数据文件
	 + /Picture 放软件运行所需的其他图片文件
	 + 窗口文件全部外置，原因是窗口文件本来就不多，避免出问题

2. 程序主要在MainWindow下完成，并利用TabView和UserControl完成分割。