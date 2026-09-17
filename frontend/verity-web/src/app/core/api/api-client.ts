import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthResponse,
  Comment,
  CommentListQuery,
  CreateCommentRequest,
  CreatePostRequest,
  LikeStatus,
  LoginRequest,
  PagedResult,
  PostDetail,
  PostListQuery,
  PostSummary,
  PostTagName,
  RegisterRequest,
  UserSummary,
} from './models';

@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/auth/register`, request);
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/auth/login`, request);
  }

  me(): Observable<UserSummary> {
    return this.http.get<UserSummary>(`${this.baseUrl}/users/me`);
  }

  listPosts(query: PostListQuery): Observable<PagedResult<PostSummary>> {
    return this.http.get<PagedResult<PostSummary>>(`${this.baseUrl}/posts`, {
      params: this.toHttpParams(query as Record<string, unknown>),
    });
  }

  getPost(id: string): Observable<PostDetail> {
    return this.http.get<PostDetail>(`${this.baseUrl}/posts/${id}`);
  }

  createPost(request: CreatePostRequest): Observable<PostDetail> {
    return this.http.post<PostDetail>(`${this.baseUrl}/posts`, request);
  }

  listComments(postId: string, query: CommentListQuery): Observable<PagedResult<Comment>> {
    return this.http.get<PagedResult<Comment>>(`${this.baseUrl}/posts/${postId}/comments`, {
      params: this.toHttpParams(query as Record<string, unknown>),
    });
  }

  addComment(postId: string, request: CreateCommentRequest): Observable<Comment> {
    return this.http.post<Comment>(`${this.baseUrl}/posts/${postId}/comments`, request);
  }

  like(postId: string): Observable<LikeStatus> {
    return this.http.put<LikeStatus>(`${this.baseUrl}/posts/${postId}/likes/me`, {});
  }

  unlike(postId: string): Observable<LikeStatus> {
    return this.http.delete<LikeStatus>(`${this.baseUrl}/posts/${postId}/likes/me`);
  }

  addTag(postId: string, tag: PostTagName, reason?: string): Observable<PostDetail> {
    return this.http.put<PostDetail>(`${this.baseUrl}/posts/${postId}/tags/${tag}`, { reason: reason ?? null });
  }

  removeTag(postId: string, tag: PostTagName): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/posts/${postId}/tags/${tag}`);
  }

  private toHttpParams(query: Record<string, unknown>): HttpParams {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value === undefined || value === null || value === '') {
        continue;
      }
      params = params.set(key, String(value));
    }
    return params;
  }
}
