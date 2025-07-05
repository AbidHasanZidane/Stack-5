using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    public TileState state { get; private set; }
    public TileCell cell { get; private set; }
    public int number { get; private set; }
    public bool locked { get; set; }

    private Image background;
    private TextMeshProUGUI text;

    private void Awake()
    {
        background = GetComponent<Image>();
        text = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void SetState(TileState state, int number, bool isPrime = false)
    {
        this.state = state;
        this.number = number;

        background.color = state.backgroundColor;
        text.color = state.textColor;
        text.text = number.ToString();
    }

    public void Spawn(TileCell cell)
    {
        if (this.cell != null)
        {
            this.cell.tile = null; // Remove reference from previous cell
        }

        this.cell = cell;
        this.cell.tile = this;

        transform.position = cell.transform.position;
    }

    public void MoveTo(TileCell targetCell)
    {
        if (cell != null)
            cell.tile = null;

        cell = targetCell;
        cell.tile = this;

        LeanTween.move(gameObject, targetCell.transform.position, 0.15f).setEaseOutQuad();
    }

    public void Merge(TileCell targetCell)
    {
        if (cell != null)
            cell.tile = null;

        cell = null;
        var targetTile = targetCell.tile;
        if (targetTile != null)
            targetTile.locked = true;

        LeanTween.move(gameObject, targetCell.transform.position, 0.15f)
            .setEaseOutQuad()
            .setOnComplete(() =>
            {
                if (targetTile != null)
                {
                    LeanTween.scale(targetTile.gameObject, Vector3.one * 1.2f, 0.15f)
                             .setEaseOutBack()
                             .setOnComplete(() => targetTile.transform.localScale = Vector3.one);
                }

                Destroy(gameObject);
            });
    }

}
