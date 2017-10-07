﻿/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace OneScript.DebugProtocol
{
    [ServiceContract(
        Namespace = "http://oscript.io/services/debugger", 
        SessionMode = SessionMode.Required,
        CallbackContract = typeof(IDebugEventListener))]
    public interface IDebuggerService
    {
        /// <summary>
        /// Разрешает потоку виртуальной машины начать выполнение скрипта
        /// Все точки останова уже установлены, все настройки сделаны
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void Execute();
        
        /// <summary>
        /// Установка точек остановки
        /// </summary>
        /// <param name="breaksToSet"></param>
        /// <returns>Возвращает установленные точки (те, которые смог установить)</returns>
        [OperationContract]
        Breakpoint[] SetMachineBreakpoints(Breakpoint[] breaksToSet);

        /// <summary>
        /// Запрашивает состояние кадров стека вызовов
        /// </summary>
        [OperationContract]
        StackFrame[] GetStackFrames();

        /// <summary>
        /// Получает значения переменных
        /// </summary>
        /// <param name="frameIndex"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        [OperationContract]
        Variable[] GetVariables(int frameIndex, int[] path);

        /// <summary>
        /// Вычисление выражения на остановленном процессе
        /// </summary>
        /// <param name="contextFrame">Кадр стека, относительно которого вычисляем</param>
        /// <param name="expression">Выражение</param>
        /// <returns>Переменная с результатом</returns>
        [OperationContract]
        Variable Evaluate(int contextFrame, string expression);

        [OperationContract(IsOneWay = true)]
        void Next();

        [OperationContract(IsOneWay = true)]
        void StepIn();

        [OperationContract(IsOneWay = true)]
        void StepOut();
    }

    public interface IDebugEventListener
    {
        [OperationContract(IsOneWay = true)]
        void ThreadStopped(int threadId, ThreadStopReason reason);

        [OperationContract(IsOneWay = true)]
        void ProcessExited(int exitCode);
    }
}
