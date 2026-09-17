import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'app-pagination',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './pagination.html',
  styleUrl: './pagination.scss',
})
export class Pagination {
  readonly page = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly pageChange = output<number>();

  protected readonly canGoPrev = computed(() => this.page() > 1);
  protected readonly canGoNext = computed(() => this.page() < this.totalPages());

  protected prev(): void {
    if (this.canGoPrev()) {
      this.pageChange.emit(this.page() - 1);
    }
  }

  protected next(): void {
    if (this.canGoNext()) {
      this.pageChange.emit(this.page() + 1);
    }
  }
}
