import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { IApiResult } from '../../core/http/api-result.model';
import { IMenuItem, IMenuItemDto } from '../models/menu-item.model';

function mapMenuItemDtoToModel(dto: IMenuItemDto): IMenuItem {
  return {
    Id: dto.id,
    ParentId: dto.parentId,
    Code: dto.code,
    Label: dto.label,
    Icon: dto.icon,
    Route: dto.route,
    DisplayOrder: dto.displayOrder,
    Children: [],
  };
}

/** Dựng cây 1 cấp từ danh sách phẳng — item không có `ParentId` khớp nào trong tập hợp = root. */
function buildMenuTree(items: IMenuItem[]): IMenuItem[] {
  const byId = new Map(items.map((item) => [item.Id, item]));
  const sorted = [...items].sort((a, b) => a.DisplayOrder - b.DisplayOrder);
  const roots: IMenuItem[] = [];

  for (const item of sorted) {
    const parent = item.ParentId ? byId.get(item.ParentId) : undefined;
    if (parent) {
      parent.Children.push(item);
    } else {
      roots.push(item);
    }
  }
  return roots;
}

/**
 * `GET /api/meta/menu` — xem doc/contracts/menu.md (CONTRACT MENU-1). Dùng cho `Sidebar` (thuộc
 * ngoại lệ "app-shell", xem doc/huong_dan/wiki-core/fe/05-component-library.md) — sidebar KHÔNG
 * hard-code menu, luôn tải động theo role hiện tại (BE đã lọc `SysMenuRole` sẵn).
 */
@Injectable({ providedIn: 'root' })
export class MenuService {
  private readonly http = inject(HttpClient);

  getMenu(): Observable<IMenuItem[]> {
    return this.http
      .get<IApiResult<IMenuItemDto[]>>('/meta/menu')
      .pipe(map((res) => buildMenuTree((res.data ?? []).map(mapMenuItemDtoToModel))));
  }
}
