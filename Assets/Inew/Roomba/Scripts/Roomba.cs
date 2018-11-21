using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO.Ports;

namespace Inew.Roomba
{
    public class Roomba
    {
        private const int m_defaultVelocity = 100;    // スピード上限は500(mm/s)
        private const int m_defaultRadius = 0x7FFF; // 直進の際の半径

        private string m_name;
        private int m_boardRate;
        private SerialPort m_serialPort;

        // ルンバの通信速度のリスト。通信の際にはkey番号を用いる
        private int[] m_boardRateList = { 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 28800, 38400, 57600, 115200 };


        // Off     : ルンバを起動した直後の状態（19200 or 115200 baud のみ）
        // Safe    : 命令は受け付けるが、段差などを検出した際に自動で停止しパッシブモードへ移行
        // Full    : 例外なくすべてのコマンド操作の命令に従う
        // Passive : ルンバのセンサー情報を読み取れる（モーター、スピーカー、ライトへのアクセスは不可）
        public enum Mode
        {
            Safe = 131,
            Full = 132,
            Passive = 133
        }

        public enum Rotation
        {
            Right = -1,
            Left = 1
        }



        public Roomba(string portName, int boardRate)
        {
            m_name = portName;
            m_boardRate = boardRate;
            m_serialPort = new SerialPort(portName, boardRate, Parity.None, 8, StopBits.One);
        }


        #region Public Method


        /// <summary>
        /// ルンバの初期化（初期設定）を行う
        /// </summary>
        public void Init()
        {
            try
            {
                OpenPort();

                Start();
                SetMode(Mode.Safe);
                SetBoardRate(m_boardRate);

                Debug.Log("Roomba Initialized.");
            }
            catch (Exception e)
            {
                Debug.Log("Connection failed ... Roomba : " + m_name);
                Debug.LogError(e.Message);
            }

        }

        /// <summary>
        /// ルンバを移動させる
        /// </summary>
        /// <param name="velocity">速度(正で前方向)</param>
        /// <param name="radius">回転半径(正で左方向)</param>
        public void Move(int velocity = m_defaultVelocity, int radius = m_defaultRadius)
        {
            byte[] velocity_bytes = BitConverter.GetBytes(velocity);
            byte[] radius_bytes = BitConverter.GetBytes(radius);

            byte[] packet = new byte[] {
            137,                // "Drive"を表すコマンド
            velocity_bytes[1],  // 速度(16ビット)の上位8ビット列
            velocity_bytes[0],  // 速度(16ビット)の下位8ビット列
            radius_bytes[1],    // 回転(16ビット)の上位8ビット列
            radius_bytes[0]     // 回転(16ビット)の下位8ビット列
        };

            DoCommand(packet);
        }

        /// <summary>
        /// ルンバを回転させる
        /// </summary>
        /// <param name="rotation">回転方向</param>
        /// <param name="velocity">回転速度</param>
        public void Rotate(Rotation rotation, int velocity = m_defaultVelocity)
        {

            byte[] velocity_bytes = BitConverter.GetBytes((UInt16)velocity);
            byte[] radius_bytes = BitConverter.GetBytes((UInt16)rotation);

            byte[] packet = new byte[] { 137, velocity_bytes[1], velocity_bytes[0], radius_bytes[1], radius_bytes[0] };

            DoCommand(packet);
        }

        /// <summary>
        /// ルンバの動きを停止します
        /// </summary>
        public void Stop()
        {
            Move(0, 0);
        }

        /// <summary>
        /// ルンバのモードを変更します
        /// </summary>
        /// <param name="mode"></param>
        public void SetMode(Mode mode)
        {
            DoCommand((byte)mode);
        }

        /// <summary>
        /// ルンバとの通信速度をセットします
        /// </summary>
        /// <param name="boardRate"></param>
        public void SetBoardRate(int boardRate = 57600)
        {
            int index = Array.IndexOf(m_boardRateList, boardRate);

            if (index >= 0)
            {
                byte[] packet = new byte[] { 129, (byte)index };
                DoCommand(packet);
            }
            else
            {
                Debug.LogWarning(boardRate + "Hz is illegal value.");
            }
        }

        /// <summary>
        /// ルンバの各ブラシを動かします
        /// </summary>
        /// <param name="mainb_direct"></param>
        /// <param name="sideb_direct"></param>
        /// <param name="mainbrush"></param>
        /// <param name="vaccum"></param>
        /// <param name="sidebrush"></param>
        /// <returns></returns>
        private byte[] Motors(bool mainb_direct, bool sideb_direct, bool mainbrush, bool vaccum, bool sidebrush)
        {
            int data = 0;
            data += (mainb_direct) ? 16 : 0;
            data += (sideb_direct) ? 8 : 0;
            data += (mainbrush) ? 4 : 0;
            data += (vaccum) ? 2 : 0;
            data += (sidebrush) ? 1 : 0;


            byte[] packet = new byte[] { 138, Convert.ToByte(data) };

            return packet;
        }

        /// <summary>
        /// ルンバを停止させ、ポートを閉じます
        /// </summary>
        public void Finish()
        {
            //Stop();
            m_serialPort.Close();
        }


        #endregion


        #region Private Method


        /// <summary>
        /// ルンバの操作を開始する
        /// </summary>
        private void Start()
        {
            DoCommand(new byte[] { 128 });
        }

        /// <summary>
        /// ルンバのポートを開き、通信路を確立します
        /// </summary>
        private void OpenPort()
        {
            try
            {
                m_serialPort.DtrEnable = false;
                m_serialPort.Handshake = Handshake.None;
                m_serialPort.RtsEnable = false;
                m_serialPort.Open();

                Debug.Log("Port Opened");
            }
            catch (Exception)
            {
                Debug.Log("Port opening failed ... Roomba : " + m_name);
                throw;
            }

        }

        /// <summary>
        /// ルンバに命令を飛ばします
        /// </summary>
        /// <param name="packet">命令を表すバイト列の配列</param>
        private void DoCommand(byte[] packet)
        {
            try
            {
                m_serialPort.Write(packet, 0, packet.Length);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        /// <summary>
        /// ルンバに命令を飛ばします
        /// </summary>
        /// <param name="packet">命令を表すバイト列</param>
        private void DoCommand(byte packet)
        {
            DoCommand(new byte[] { packet });
        }

        /// <summary>
        /// ルンバに命令を飛ばします
        /// </summary>
        /// <param name="packet">命令を表すバイト列のint型</param>
        private void DoCommand(int packet)
        {
            DoCommand(new byte[] { (byte)packet });
        }

        #endregion

    }
}
