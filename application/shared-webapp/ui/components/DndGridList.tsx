/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/index.html?path=/docs/gridlist--docs
 */
import { DropIndicator, isTextDropItem, useDragAndDrop } from "react-aria-components";
import { useListData } from "react-stately";
import { GridList, GridListItem } from "./GridList";

interface ItemData {
  id: string;
  name: string;
}

interface DndGridListProps<T extends ItemData[]> {
  initialItems: T;
  "aria-label": string;
  dataTypeId: string;
}

export function DndGridList<T extends ItemData[]>({
  dataTypeId,
  initialItems,
  ...props
}: Readonly<DndGridListProps<T>>) {
  const list = useListData({
    initialItems: initialItems
  });

  const { dragAndDropHooks } = useDragAndDrop({
    // Provide drag data in a custom format as well as plain text.
    getItems(keys) {
      return [...keys].map((key) => {
        const item = list.getItem(key);
        return {
          [dataTypeId]: JSON.stringify(item),
          "text/plain": item.name
        };
      });
    },

    // Accept drops with the custom format.
    acceptedDragTypes: [dataTypeId],

    // Ensure items are always moved rather than copied.
    getDropOperation: () => "move",

    // Handle drops between items from other lists.
    async onInsert(e) {
      const processedItems = await Promise.all(
        e.items.filter(isTextDropItem).map(async (item) => JSON.parse(await item.getText(dataTypeId)))
      );
      if (e.target.dropPosition === "before") {
        list.insertBefore(e.target.key, ...processedItems);
      } else if (e.target.dropPosition === "after") {
        list.insertAfter(e.target.key, ...processedItems);
      }
    },

    // Handle drops on the collection when empty.
    async onRootDrop(e) {
      const processedItems = await Promise.all(
        e.items.filter(isTextDropItem).map(async (item) => JSON.parse(await item.getText(dataTypeId)))
      );
      list.append(...processedItems);
    },

    // Handle reordering items within the same list.
    onReorder(e) {
      if (e.target.dropPosition === "before") {
        list.moveBefore(e.target.key, e.keys);
      } else if (e.target.dropPosition === "after") {
        list.moveAfter(e.target.key, e.keys);
      }
    },

    // Remove the items from the source list on drop
    // if they were moved to a different list.
    onDragEnd(e) {
      if (e.dropOperation === "move" && !e.isInternal) {
        list.remove(...e.keys);
      }
    },

    renderDropIndicator(target) {
      return (
        <DropIndicator
          target={target}
          className={({ isDropTarget }) => `${isDropTarget ? "z-10 border border-ring" : ""}`}
        />
      );
    },

    renderDragPreview(items) {
      return (
        <div className="flex gap-8 rounded-md border border-border bg-muted/50 px-2 py-1 text-sm">
          {items[0]["text/plain"]}
          <span className="rounded-md bg-muted px-2">{items.length}</span>
        </div>
      );
    }
  });

  return (
    <GridList
      aria-label={props["aria-label"]}
      selectionMode="multiple"
      selectedKeys={list.selectedKeys}
      onSelectionChange={list.setSelectedKeys}
      items={list.items}
      dragAndDropHooks={dragAndDropHooks}
      renderEmptyState={() => (
        <div className="flex h-full items-center justify-center p-4 text-sm">Drop items here</div>
      )}
    >
      {(item) => <GridListItem>{item.name}</GridListItem>}
    </GridList>
  );
}
