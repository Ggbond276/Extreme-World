// RayMix Libs - RayMix's .Net Libs
// Copyright 2018 Ray@raymix.net.  All rights reserved.
// https://www.raymix.net
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following disclaimer
// in the documentation and/or other materials provided with the
// distribution.
//     * Neither the name of RayMix.net. nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.


using System;
using System.Runtime.InteropServices;
class Time
{

    // ==============================================================================
    // 引入 Windows 底层 API：为了获取极高精度的时间（绕过 C# 默认的低精度 DateTime）
    // ==============================================================================
    [DllImport("kernel32.dll")]
    static extern bool QueryPerformanceCounter([In, Out] ref long lpPerformanceCount);
    [DllImport("kernel32.dll")]
    static extern bool QueryPerformanceFrequency([In, Out] ref long lpFrequency);

    /// <summary>
    /// 静态构造函数：在服务器刚启动、这个类第一次被用到时执行一次
    /// </summary>
    static Time()
    {
        startupTicks = ticks; // 记录下服务器启动那一瞬间的时间戳
    }


    private static long _frameCount = 0;

    /// <summary>
    /// 自服务器（游戏）启动以来，已经经过的总帧数（只读）。
    /// </summary>
    public static long frameCount { get { return _frameCount; } }

    /// <summary>
    /// 记录服务器启动时的滴答数
    /// </summary>
    static long startupTicks = 0;

    /// <summary>
    /// 记录 CPU 计时器的频率
    /// </summary>
    static long freq = 0;

    /// <summary>
    /// 获取当前的高精度时间戳（Tick数）。
    /// 这是整个类的核心，底层的核心算法全在计算这个精确的 Tick。
    /// </summary>
    static public long ticks
    {
        get
        {
            long f = freq;

            if (f == 0)
            {
                if (QueryPerformanceFrequency(ref f))
                {
                    freq = f;
                }
                else
                {
                    freq = -1;
                }
            }
            if (f == -1)
            {
                return Environment.TickCount * 10000;
            }
            long c = 0;
            QueryPerformanceCounter(ref c);
            return (long)(((double)c) * 1000 * 10000 / ((double)f));
        }
    }

    private static long lastTick = 0;
    private static float _deltaTime = 0;

    /// <summary>
    /// 完成上一帧所花费的时间，以秒为单位（只读）。
    /// 比如服务器卡顿了一下，这个值就会变大。常用于平滑移动计算。
    /// </summary>
    public static float deltaTime
    {
        get
        {
            return _deltaTime;
        }
    }


    private static float _time = 0;
    /// <summary>
    /// 【我们做组队状态同步最核心要用的属性】
    /// 此帧开始时的时间（只读）。这是自游戏（或服务器）启动以来的时间，以秒为单位。
    /// 给队伍盖的“时间戳”用的就是它！
    /// </summary>
    public static float time
    {
        get
        {
            return _time;
        }
    }


    /// <summary>
    /// 自服务器启动以来的真实物理时间，以秒为单位（只读）。
    /// </summary>
    public static float realtimeSinceStartup
    {
        get
        {
            long _ticks = ticks;
            return (_ticks - startupTicks) / 10000000f;
        }
    }
    /// <summary>
    /// 核心驱动方法：必须在服务器的主循环（比如 Update 函数）中每帧调用一次！
    /// 它的作用就是不断地往前推移时间，更新 time 和 deltaTime。
    /// </summary>
    public static void Tick()
    {
        long _ticks = ticks;


        _frameCount++;
        if (_frameCount == long.MaxValue)
            _frameCount = 0;

        if (lastTick == 0) lastTick = _ticks;
        _deltaTime = (_ticks - lastTick) / 10000000f;
        _time = (_ticks - startupTicks) / 10000000f;
        lastTick = _ticks;
    }
}