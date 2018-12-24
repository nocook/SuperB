using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SuperBMgr
{
    public static class LogHelper
    {
        //Logger对象代表与当前类相关联的日志消息的来源 
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static void SetLogName(string strName)
        {
            logger = LogManager.GetLogger(strName);
        }

        public static void Debug(string format, params object[] objs)
        {
            logger.Debug(string.Format(format, objs));
        }

        public static void Debug(string content)
        {
            logger.Debug(content);
        }

        public static void Warn(string format, params object[] objs)
        {
            logger.Warn(string.Format(format, objs));
        }

        public static void Warn(string content)
        {
            logger.Warn(content);
        }

        public static void Trace(string format, params object[] objs)
        {
            logger.Trace(string.Format(format, objs));
        }

        public static void Trace(string content)
        {
            logger.Trace(content);
        }

        public static void Info(string format, params object[] objs)
        {
            logger.Info(string.Format(format, objs));
        }

        public static void Info(string content)
        {
            logger.Info(content);
        }

        public static void Error(string format, params object[] objs)
        {
            logger.Error(string.Format(format, objs));
        }

        public static void Error(string content)
        {
            logger.Error(content + "\r\n");
        }

        public static string GetStackInfoStr(int index)
        {
            StackTrace st = new StackTrace(new StackFrame(true));
            var sfs = st.GetFrames();
            return "s";// $"File: {sf.GetFileName()}\r\nMethod: {sf.GetMethod().Name}\r\nLine: { sf.GetFileLineNumber()}";
        }

        public static void Error(Exception ex)
        {
            logger.Error(ex.ToString());
        }
    }
}
