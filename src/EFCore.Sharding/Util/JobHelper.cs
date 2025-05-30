﻿using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace EFCore.Sharding
{
    /// <summary>
    /// 任务帮助类
    /// </summary>
    public static class JobHelper
    {
        #region 私有成员

        private static IScheduler __scheduler;
        private static readonly object _lock = new();

        private static IScheduler _scheduler
        {
            get
            {
                if (__scheduler == null)
                {
                    lock (_lock)
                    {
                        if (__scheduler == null)
                        {
                            __scheduler = AsyncHelper.RunSync(() => StdSchedulerFactory.GetDefaultScheduler());
                            AsyncHelper.RunSync(() => __scheduler.Start());
                        }
                    }
                }

                return __scheduler;
            }
        }

        private static ConcurrentDictionary<string, Action> _jobs { get; }
            = new ConcurrentDictionary<string, Action>();

        #endregion 私有成员

        #region 外部接口

        /// <summary>
        /// 设置一个时间间隔的循环操作
        /// </summary>
        /// <param name="action"> 执行的操作 </param>
        /// <param name="timeSpan"> 时间间隔 </param>
        /// <param name="key"> 任务key 长度大于5-200字符串 </param>
        /// <returns> 任务标识Id </returns>
        public static string SetIntervalJob(Action action, TimeSpan timeSpan, string key = "")
        {
            if (key.IsNullOrEmpty())
            {
                key = Guid.NewGuid().ToString();
            }
            else
            {
                RemoveJob(key);//删除
            }

            if (!_jobs.ContainsKey(key))
            {
                _jobs[key] = action;
                IJobDetail job = JobBuilder.Create<Job>()
                   .WithIdentity(key)
                   .Build();
                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity(key)
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithInterval(timeSpan).RepeatForever())
                    .Build();
                _ = AsyncHelper.RunSync(() => _scheduler.ScheduleJob(job, trigger));
            }
            return key;
        }

        /// <summary>
        /// 设置每天定时任务
        /// </summary>
        /// <param name="action"> 执行的任务 </param>
        /// <param name="h"> 时 </param>
        /// <param name="m"> 分 </param>
        /// <param name="s"> 秒 </param>
        /// <param name="key"> 任务key 长度大于5-200字符串 </param>
        /// <returns> 任务标识Id </returns>
        public static string SetDailyJob(Action action, int h, int m, int s, string key = "")
        {
            if (key.IsNullOrEmpty())
            {
                key = Guid.NewGuid().ToString();
            }
            else
            {
                RemoveJob(key);//删除
            }
            if (!_jobs.ContainsKey(key))
            {
                _jobs[key] = action;
                IJobDetail job = JobBuilder.Create<Job>()
                   .WithIdentity(key)
                   .Build();
                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity(key)
                    .StartNow()
                    .WithCronSchedule($"{s} {m} {h} * * ?")//每天定时
                    .Build();
                _ = AsyncHelper.RunSync(() => _scheduler.ScheduleJob(job, trigger));
            }
            return key;
        }

        /// <summary>
        /// 设置延时任务,仅执行一次
        /// </summary>
        /// <param name="action"> 执行的操作 </param>
        /// <param name="delay"> 延时时间 </param>
        /// <param name="key"> 任务key 长度大于5-200字符串 </param>
        /// <returns> 任务标识Id </returns>
        public static string SetDelayJob(Action action, TimeSpan delay, string key = "")
        {
            if (key.IsNullOrEmpty())
            {
                key = Guid.NewGuid().ToString();
            }
            else
            {
                RemoveJob(key);//删除
            }
            if (!_jobs.ContainsKey(key))
            {
                #region 执行完成删除任务

                action += () =>
                {
                    RemoveJob(key);
                };

                #endregion 执行完成删除任务

                _jobs[key] = action;

                IJobDetail job = JobBuilder.Create<Job>()
                   .WithIdentity(key)
                   .Build();
                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity(key)
                    .StartAt(DateTime.Now + delay)
                    .WithSimpleSchedule(x => x.WithRepeatCount(0).WithInterval(TimeSpan.FromSeconds(10)))
                    .Build();
                _ = AsyncHelper.RunSync(() => _scheduler.ScheduleJob(job, trigger));
            }
            return key;
        }

        /// <summary>
        /// 通过表达式创建任务 表达式规则参考:http://www.jsons.cn/quartzcron/
        /// </summary>
        /// <param name="action"> 执行的操作 </param>
        /// <param name="cronExpression"> 表达式 </param>
        /// <param name="key"> 任务key 长度大于5-200字符串 </param>
        /// <returns> </returns>
        public static string SetCronJob(Action action, string cronExpression, string key = "")
        {
            if (key.IsNullOrEmpty())
            {
                key = Guid.NewGuid().ToString();
            }
            else
            {
                RemoveJob(key);//删除
            }
            if (!_jobs.ContainsKey(key))
            {
                _jobs[key] = action;
                IJobDetail job = JobBuilder.Create<Job>()
                   .WithIdentity(key)
                   .Build();
                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity(key)
                    .StartNow()
                    .WithCronSchedule(cronExpression)
                    .Build();
                _ = AsyncHelper.RunSync(() => _scheduler.ScheduleJob(job, trigger));
            }
            return key;
        }

        /// <summary>
        /// 删除任务
        /// </summary>
        /// <param name="jobId"> 任务标识Id </param>
        public static void RemoveJob(string jobId)
        {
            if (_jobs.ContainsKey(jobId))
            {
                _ = AsyncHelper.RunSync(() => _scheduler.DeleteJob(new JobKey(jobId)));
                if (!_jobs.TryRemove(jobId, out _))
                {
                    _ = _jobs.TryRemove(jobId, out _);
                }
            }
        }

        #endregion 外部接口

        #region 内部类

        private class Job : IJob
        {
            public async Task Execute(IJobExecutionContext context)
            {
                await Task.Run(() =>
                {
                    string jobName = context.JobDetail.Key.Name;
                    if (_jobs.ContainsKey(jobName))
                    {
                        _jobs[jobName]?.Invoke();
                    }
                });
            }
        }

        #endregion 内部类
    }
}
