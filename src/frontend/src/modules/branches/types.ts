export interface Branch {
  id: number
  name: string
  address: string
  phone: string | null
  isActive: boolean
  activeUserCount: number
}

export interface SaveBranchRequest {
  name: string
  address: string
  phone: string | null
}
