import { ChangeDetectionStrategy, Component, OnInit, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { PostListQuery, PostSortBy, SortDirection } from '../../../core/api/models';

export type TagFilterOption = 'any' | 'MisleadingOrFalse' | 'untagged';

@Component({
  selector: 'app-post-filters',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './post-filters.html',
  styleUrl: './post-filters.scss',
})
export class PostFilters implements OnInit {
  private readonly fb = inject(FormBuilder);

  readonly initialValue = input<PostListQuery>({});
  readonly apply = output<PostListQuery>();
  readonly reset = output<void>();

  protected readonly form = this.fb.nonNullable.group({
    dateFrom: [''],
    dateTo: [''],
    author: [''],
    tagOption: ['any' as TagFilterOption],
    sortBy: ['createdAt' as PostSortBy],
    sortDirection: ['desc' as SortDirection],
  });

  ngOnInit(): void {
    const initial = this.initialValue();
    this.form.patchValue({
      dateFrom: initial.dateFrom?.slice(0, 10) ?? '',
      dateTo: initial.dateTo?.slice(0, 10) ?? '',
      author: initial.author ?? '',
      tagOption: initial.untagged ? 'untagged' : initial.tag ? 'MisleadingOrFalse' : 'any',
      sortBy: initial.sortBy ?? 'createdAt',
      sortDirection: initial.sortDirection ?? 'desc',
    });
  }

  protected submit(): void {
    const value = this.form.getRawValue();
    this.apply.emit({
      dateFrom: value.dateFrom || undefined,
      dateTo: value.dateTo || undefined,
      author: value.author || undefined,
      tag: value.tagOption === 'MisleadingOrFalse' ? 'MisleadingOrFalse' : undefined,
      untagged: value.tagOption === 'untagged',
      sortBy: value.sortBy,
      sortDirection: value.sortDirection,
    });
  }

  protected resetFilters(): void {
    this.form.reset({
      dateFrom: '',
      dateTo: '',
      author: '',
      tagOption: 'any',
      sortBy: 'createdAt',
      sortDirection: 'desc',
    });
    this.reset.emit();
  }
}
