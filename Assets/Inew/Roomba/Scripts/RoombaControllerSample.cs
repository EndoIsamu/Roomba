using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace Inew.Roomba
{
    public class RoombaControllerSample : MonoBehaviour
    {
        private Dictionary<string, Roomba> m_roombaDic;

        [SerializeField, Tooltip("ルンバの識別子リスト")]
        private List<string> ports;

        [SerializeField, Tooltip("ルンバの移動速度(デフォルトで100)")]
        private int speed = 100;

        [SerializeField, Tooltip("ルンバの回転半径(デフォルトで200)")]
        private int radius = 200;

        // [SerializeField, Tooltip("ルンバとの通信速度(デフォルトで57600)")]
        // ボードレートは変更する必要ありません
        private int boardRate = 57600;

        // こいつ何者？
        private bool Double_digid = false;




        /// <summary>
        /// 全てのルンバの初期化
        /// </summary>
        void Awake()
        {
            m_roombaDic = new Dictionary<string, Roomba>();

            for (int i = 0; i < ports.Count; i++)
            {
                string portName = (Double_digid) ? "\\\\.\\" + ports[i] : ports[i];

                m_roombaDic.Add(portName, new Roomba(portName, boardRate));
            }

            // 全てのルンバの初期化（通信）を並列で行う
            Parallel.ForEach(m_roombaDic, roomba =>
            {
                roomba.Value.Init();
            });

        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    // 右斜め前
                    MoveAll(speed, -radius);
                }
                else if (Input.GetKey(KeyCode.LeftArrow))
                {
                    // 左斜め前
                    MoveAll(speed, radius);
                }
                else
                {
                    // 前進
                    MoveAll(speed);
                }
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    // 右斜め後ろ
                    MoveAll(-speed, -radius);
                }
                else if (Input.GetKey(KeyCode.LeftArrow))
                {
                    // 左斜め後ろ
                    MoveAll(-speed, radius);
                }
                else
                {
                    // 後退
                    MoveAll(-speed);
                }
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                // 右回転
                RotateAll(Roomba.Rotation.Right, speed);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                // 左回転
                RotateAll(Roomba.Rotation.Left, speed);
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                // 停止
                StopAll();
            }

        }

        /// <summary>
        /// デストラクタ
        /// </summary>
        void OnDestroy()
        {
            Parallel.ForEach(m_roombaDic, roomba =>
            {
                roomba.Value.Finish();
                Debug.Log(roomba.Key + " Finished.");
            });

        }


        /// <summary>
        /// 全てのルンバに同じ移動をさせる
        /// </summary>
        /// <param name="velocity">速度</param>
        /// <param name="radius">回転半径</param>
        private void MoveAll(int velocity = 100, int radius = 0x7FFF)
        {
            Parallel.ForEach(m_roombaDic, roomba =>
            {
                roomba.Value.Move(velocity: velocity, radius: radius);
                Debug.Log(roomba.Key + " is Moving." + "velocity: " + velocity + "radius: " + radius);
            });
        }

        /// <summary>
        /// 全てのルンバを同じ方向に回転させる
        /// </summary>
        /// <param name="rotation">回転方向</param>
        /// <param name="velocity">速度</param>
        private void RotateAll(Roomba.Rotation rotation, int velocity = 100)
        {
            Parallel.ForEach(m_roombaDic, roomba =>
            {
                roomba.Value.Rotate(rotation, velocity: velocity);
                Debug.Log(roomba.Key + " is Rotating." + "Rotation: " + rotation.ToString() + "Velocity: " + velocity);
            });
        }

        /// <summary>
        /// 全てのルンバを停止させる
        /// </summary>
        private void StopAll()
        {
            Parallel.ForEach(m_roombaDic, roomba =>
            {
                roomba.Value.Stop();
                Debug.Log(roomba.Key + " Stopped.");
            });
        }

    }
}
