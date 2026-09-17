import { UserSummary } from './user.model';

export interface Comment {
  id: string;
  postId: string;
  body: string;
  author: UserSummary;
  createdAt: string;
}

export interface CreateCommentRequest {
  body: string;
}

export interface CommentListQuery {
  page?: number;
  pageSize?: number;
  sortDirection?: 'asc' | 'desc';
}
