select miv.grandparent_title, miv.parent_index, miv.`index`, miv.`viewed_at`
from metadata_item_views miv
where miv.parent_index > 0
  and miv.metadata_type = 4
  and (miv.viewed_at > date('now', '-1 day') and miv.viewed_at < datetime('now', '-4 hours', '-5 minutes'))
  and miv.account_id = 1
order by miv.grandparent_title, miv.parent_index, miv.`index`;