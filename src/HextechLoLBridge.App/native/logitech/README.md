把 Logitech 官方 LED Illumination SDK 里的 LogitechLedEnginesWrapper.dll 放到这个目录。

调试输出目录通常会复制到：
- bin\Debug\net8.0-windows10.0.19041.0\win-x64\native\logitech\

当前版本只做了：
1. DLL 探测
2. LogiLedInit 初步初始化尝试
3. 一帧静态深蓝占位灯色输出
4. 退出时尝试恢复原灯效
