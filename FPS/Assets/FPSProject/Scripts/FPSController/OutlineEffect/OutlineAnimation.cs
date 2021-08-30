using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using cakeslice;

namespace cakeslice
{
    public class OutlineAnimation : MonoBehaviour
    {
        [SerializeField] bool m_IsAnimationEnabled = true;

        OutlineEffect m_OutlineEffect;

        Color color, outlineColor;

        bool pingPong = false, m_IsAnimationHovered = false;

        // Use this for initialization
        void Start()
        {
            m_OutlineEffect = GetComponent<OutlineEffect>();
        }

        // Update is called once per frame
        void Update()
        {
            if (m_IsAnimationEnabled && !m_IsAnimationHovered)
            {
                color = m_OutlineEffect.lineColor0;

                if (pingPong)
                {
                    color.a += Time.deltaTime;

                    if (color.a >= 1)
                        pingPong = false;
                }
                else
                {
                    color.a -= Time.deltaTime;

                    if (color.a <= 0)
                        pingPong = true;
                }

                color.a = Mathf.Clamp01(color.a);
                m_OutlineEffect.lineColor0 = color;
                m_OutlineEffect.UpdateMaterialsPublicProperties();
            }
        }

        public void HoverHandler(bool isHovering)
        {
            m_IsAnimationHovered = isHovering;
            m_OutlineEffect.useFillColor = isHovering;

            if (isHovering)
            {
                outlineColor = m_OutlineEffect.lineColor1;
            }
            else
            {
                outlineColor = color;
            }

            m_OutlineEffect.lineColor0 = outlineColor;
            m_OutlineEffect.UpdateMaterialsPublicProperties();
        }
    }
}