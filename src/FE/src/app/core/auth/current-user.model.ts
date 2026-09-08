// Wire (DTO) — camelCase, khớp `CurrentUserInfo` phía BE (doc/contracts/auth.md).
export interface ICurrentUserDto {
  id: string;
  userName: string;
  // Nullable phía BE (`CurrentUserInfo.Email`) — chốt sau một lượt core-reviewer (finding F3).
  // Khu tài liệu audit đã xoá nên không còn đường dẫn để trỏ tới; ràng buộc thật nằm ở
  // doc/contracts/auth.md.
  email: string | null;
  fullName: string;
  roles: string[];
  mustChangePassword: boolean;
}

// Model app — PascalCase + prefix I (doc/huong_dan/quy-uoc/fe-api-client.md).
export interface ICurrentUser {
  Id: string;
  UserName: string;
  Email: string | null;
  FullName: string;
  Roles: string[];
  MustChangePassword: boolean;
}

export function mapCurrentUserDtoToModel(dto: ICurrentUserDto): ICurrentUser {
  return {
    Id: dto.id,
    UserName: dto.userName,
    Email: dto.email,
    FullName: dto.fullName,
    Roles: dto.roles,
    MustChangePassword: dto.mustChangePassword,
  };
}
