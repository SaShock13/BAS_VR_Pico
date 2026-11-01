using UnityEngine;
using UnityEngine.UI;

public class AssemblyMenuView : MonoBehaviour
{
    [SerializeField] private Button[] categories;
    [SerializeField] private GameObject[] groups;
    [SerializeField] private Button previous;
    [SerializeField] private Button next;
    [SerializeField] private ObjectRowManager[] rows;
    private int selected;

    private void Awake()
    {
        for (int i = 0; i < categories.Length; i++)
        {
            int index = i;
            categories[i].onClick.AddListener(() => SelectCategory(index));
        }
        previous.onClick.AddListener(PreviousPage);
        next.onClick.AddListener(NextPage);
        SelectCategory(0);
    }

    public void SelectCategory(int index)
    {
        if (index < 0 || index >= categories.Length) return;
        selected = index;
        for (int i = 0; i < groups.Length; i++) groups[i].SetActive(i == index);
        if (rows != null && index < rows.Length && rows[index] != null) rows[index].RefreshLayout();
    }

    public void PreviousPage()
    {
        if (rows != null && selected < rows.Length && rows[selected] != null) rows[selected].PreviousPage();
    }

    public void NextPage()
    {
        if (rows != null && selected < rows.Length && rows[selected] != null) rows[selected].NextPage();
    }
}
