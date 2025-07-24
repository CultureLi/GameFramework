using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Test.TestScripts.Runtime.ConfigTest
{
    using UnityEngine;
    using UnityEngine.UI;
    using System.Collections.Generic;
    using TMPro;

    public class MarqueeScrollVirtualized : MonoBehaviour
    {
        public RectTransform content;
        public ScrollRect scrollRect;
        public RectTransform itemPrefab;

        public int totalItems = 20;
        public int visibleCount = 12;
        public float itemHeight = 100f;
        public float speed = 100f;

        private List<ItemComp> itemPool = new List<ItemComp>();
        private int firstDataIndex = 0;

        private bool lastInited;

        void Start()
        {
            int poolSize = Math.Min(visibleCount, totalItems);

            // 创建池子
            for (int i = 0; i < poolSize; i++)
            {
                var item = Instantiate(itemPrefab, content);
                var comp = item.gameObject.AddComponent<ItemComp>();
                comp.idx = i;
                itemPool.Add(comp);
            }

            ResetPos();
        }

        private void ResetPos()
        {
            firstDataIndex = 0;
            var poolSize = itemPool.Count;
            for (int i = 0; i < poolSize; i++)
            {
                var item = itemPool[i];
                item.idx = i;
                item.name = "Item_" + i;
                item.gameObject.SetActive(true);
                SetItemPosition(item, i);
                SetItemData(item, i);
            }
        }

        void Update()
        {
            content.anchoredPosition += Vector2.up * speed * Time.deltaTime;

            // 检查最上方 Item 是否超出 Viewport 顶部
            var firstItem = itemPool[0];
            /*Vector3[] corners = new Vector3[4];
            firstItem.GetWorldCorners(corners);
            float itemTopY = corners[1].y;*/

            var itemTopY = firstItem.transform.position.y;

            float viewportTopY = scrollRect.viewport.position.y;

            if (itemTopY - itemHeight >= viewportTopY)
            {
                if (firstItem.idx == totalItems - 1)
                {
                    lastInited = false;
                    content.anchoredPosition = Vector2.zero;
                    ResetPos();
                    return;
                }

                // 将第一个 Item 移到 Content 底部
                itemPool.RemoveAt(0);
                itemPool.Add(firstItem);
                if (lastInited)
                {
                    firstItem.gameObject.SetActive(false);
                }
                else
                {
                    float newY = (itemPool[itemPool.Count - 2].transform as RectTransform).anchoredPosition.y - itemHeight;
                    (firstItem.transform as RectTransform).anchoredPosition = new Vector2(0, newY);

                    firstDataIndex = firstDataIndex + 1;
                    var idx = (firstDataIndex + itemPool.Count - 1);
                    SetItemData(firstItem, idx);
                }
            }
        }

        void SetItemPosition(ItemComp item, int index)
        {
            (item.transform as RectTransform).anchoredPosition = new Vector2(0, -index * itemHeight);
        }

        void SetItemData(ItemComp item, int dataIndex)
        {
            //最后一个
            if (dataIndex == totalItems - 1)
            {
                lastInited = true;
            }
            item.idx = dataIndex;
            item.GetComponentInChildren<TextMeshProUGUI>().text = "Item " + dataIndex;
        }
    }

}

