using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    public interface IFsmModule
    {
        /// <summary>
        /// 有限状态机数量
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 检查状态机是否存在
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        bool ContainsFsm<T>() where T : class;

        /// <summary>
        /// 检查状态机是否存在
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        bool ContainsFsm(Type type);

        /// <summary>
        /// 检查状态机是否存在
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">状态机名称</param>
        /// <returns></returns>
        bool ContainsFsm<T>(string name) where T : class;

        /// <summary>
        /// 检查状态机是否存在
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name">状态机名称</param>
        /// <returns></returns>
        bool ContainsFsm(Type type, string name);

        /// <summary>
        /// 获取状态机
        /// </summary>
        /// <typeparam name="T">状态机持有者类型</typeparam>
        /// <returns></returns>
        IFsm<T> GetFsm<T>() where T : class;

        /// <summary>
        /// 获取状态机
        /// </summary>
        /// <param name="ownerType">状态机持有者类型</param>
        /// <returns></returns>
        BaseFsm GetFsm(Type ownerType);

        /// <summary>
        /// 获取状态机
        /// </summary>
        /// <typeparam name="T">状态机持有者类型</typeparam>
        /// <param name="name">状态机名称</param>
        /// <returns></returns>
        IFsm<T> GetFsm<T>(string name) where T : class;

        /// <summary>
        /// 获取状态机
        /// </summary>
        /// <param name="ownerType">状态机持有者类型</param>
        /// <param name="name">状态机名称</param>
        /// <returns></returns>
        BaseFsm GetFsm(Type ownerType, string name);

        /// <summary>
        /// 获取所有有限状态机。
        /// </summary>
        /// <returns>所有有限状态机。</returns>
        BaseFsm[] GetAllFsms();

        /// <summary>
        /// 获取所有有限状态机。
        /// </summary>
        /// <param name="results">所有有限状态机。</param>
        void GetAllFsms(List<BaseFsm> results);

        /// <summary>
        /// 创建有限状态机
        /// </summary>
        /// <param name="owner">持有者</param>
        /// <param name="states">状态数组</param>
        /// <typeparam name="T">持有者类型</typeparam>
        /// <returns></returns>
        IFsm<T> CreateFsm<T>(T owner, params IFsmState<T>[] states) where T : class;

        /// <summary>
        /// 创建有限状态机
        /// </summary>
        /// <param name="name">状态机名称</param>
        /// <param name="owner">持有者</param>
        /// <param name="states">状态数组</param>
        /// <typeparam name="T">持有者类型</typeparam>
        /// <returns></returns>
        IFsm<T> CreateFsm<T>(string name, T owner, params IFsmState<T>[] states) where T : class;

        /// <summary>
        /// 创建有限状态机
        /// </summary>
        /// <param name="owner">持有者</param>
        /// <param name="states">状态数组</param>
        /// <typeparam name="T">持有者类型</typeparam>
        /// <returns></returns>
        IFsm<T> CreateFsm<T>(T owner, List<IFsmState<T>> states) where T : class;

        /// <summary>
        /// 创建有限状态机
        /// </summary>
        /// <param name="name">状态机名称</param>
        /// <param name="owner">持有者</param>
        /// <param name="states">状态数组</param>
        /// <typeparam name="T">持有者类型</typeparam>
        /// <returns></returns>
        IFsm<T> CreateFsm<T>(string name, T owner, List<IFsmState<T>> states) where T : class;

        /// <summary>
        /// 销毁有限状态机
        /// </summary>
        /// <typeparam name="T">持有者类型</typeparam>
        /// <returns></returns>
        bool DestroyFsm<T>() where T : class;

        /// <summary>
        /// 销毁有限状态机
        /// </summary>
        /// <param name="type">持有者类型</param>
        /// <returns></returns>
        bool DestroyFsm(Type type);

        /// <summary>
        /// 销毁有限状态机
        /// </summary>
        /// <typeparam name="T">持有者类型</typeparam>
        /// <param name="name">状态机名称</param>
        /// <returns></returns>
        bool DestroyFsm<T>(string name) where T : class;

        /// <summary>
        /// 销毁有限状态机
        /// </summary>
        /// <param name="type">持有者类型</param>
        /// <param name="name">状态机名称</param>
        /// <returns></returns>
        bool DestroyFsm(Type type, string name);

        /// <summary>
        /// 销毁有限状态机
        /// </summary>
        /// <param name="fsm">状态机</param>
        /// <typeparam name="T">持有者类型</typeparam>
        /// <returns></returns>
        bool DestroyFsm<T>(IFsm<T> fsm) where T : class;

        /// <summary>
        /// 销毁有限状态机
        /// </summary>
        /// <param name="fsm">状态机</param>
        /// <returns></returns>
        bool DestroyFsm(BaseFsm fsm);
    }
}