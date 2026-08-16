import { IUser, IUserDto, IUserPagedList, IUserPagedListDto } from '../models/quan-tri-nguoi-dung.model';

export function mapUserDtoToModel(dto: IUserDto): IUser {
  return {
    Id: dto.id,
    UserName: dto.userName,
    Email: dto.email,
    FullName: dto.fullName,
    Roles: dto.roles,
    IsLocked: dto.isLocked,
    MustChangePassword: dto.mustChangePassword,
    DateCreate: dto.dateCreate,
  };
}

export function mapUserPagedListDtoToModel(dto: IUserPagedListDto): IUserPagedList {
  return {
    Items: dto.items.map(mapUserDtoToModel),
    Total: dto.total,
    Page: dto.page,
    PageSize: dto.pageSize,
  };
}
